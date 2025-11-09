import { useEffect, useMemo, useState } from 'react'
import { apiRequest } from '../api/httpClient'
import {
  FIELD_DATA_TYPES,
  getFieldTypeLabel,
  requiresEnumValues,
  requiresReferenceDirectory,
} from '../constants/fieldDataTypes'

const newFieldTemplate = {
  name: '',
  columnName: '',
  dataType: FIELD_DATA_TYPES[0].value,
  isCollection: false,
  referenceDirectoryTypeId: '',
  enumValues: '',
}

export function TypeFieldManager({
  apiKey,
  type,
  directoryTypes,
  onBack,
  onUpdated,
  groups = [],
}) {
  const [fields, setFields] = useState(
    (type.fields ?? []).map(normalizeFieldShape),
  )
  const [error, setError] = useState('')
  const [addError, setAddError] = useState('')
  const [addFieldDraft, setAddFieldDraft] = useState(newFieldTemplate)
  const [adding, setAdding] = useState(false)
  const [editingFieldId, setEditingFieldId] = useState(null)
  const [editDraft, setEditDraft] = useState({ name: '', order: 1, isRequired: false })
  const [editing, setEditing] = useState(false)
  const [groupDraft, setGroupDraft] = useState(type.directoryGroupId ?? '')
  const [groupSaving, setGroupSaving] = useState(false)
  const [groupError, setGroupError] = useState('')
  const [groupSuccess, setGroupSuccess] = useState('')
  const currentGroupId = type.directoryGroupId ?? ''
  const isGroupDirty = groupDraft !== currentGroupId

  useEffect(() => {
    setFields((type.fields ?? []).map(normalizeFieldShape))
  }, [type])

  useEffect(() => {
    setGroupDraft(type.directoryGroupId ?? '')
    setGroupError('')
    setGroupSuccess('')
  }, [type.id, type.directoryGroupId])
  
  const sortedFields = useMemo(
    () => [...fields].sort((a, b) => (a.order ?? 0) - (b.order ?? 0)),
    [fields],
  )

  const handleAddDraftChange = (event) => {
    const { name, value, type: inputType, checked } = event.target
    setAddFieldDraft((prev) => ({
      ...prev,
      [name]:
        inputType === 'checkbox'
          ? checked
          : name === 'dataType'
            ? Number(value)
            : value,
    }))
  }

  const resetAddFieldDraft = () => {
    setAddFieldDraft(newFieldTemplate)
    setAddError('')
  }

  const handleGroupSubmit = async (event) => {
    event.preventDefault()
    setGroupError('')
    setGroupSuccess('')
    try {
      setGroupSaving(true)
      await apiRequest(`/api/directory-types/${type.id}/group`, {
        method: 'PUT',
        apiKey,
        body: { directoryGroupId: groupDraft || null },
      })
      setGroupSuccess('Группа обновлена')
      await onUpdated?.()
    } catch (requestError) {
      setGroupError(requestError.message ?? 'Не удалось обновить группу')
    } finally {
      setGroupSaving(false)
    }
  }

  const handleAddField = async (event) => {
    event.preventDefault()
    setAddError('')

    if (!addFieldDraft.name.trim() || !addFieldDraft.columnName.trim()) {
      setAddError('Укажите название и имя столбца')
      return
    }

    if (requiresReferenceDirectory(addFieldDraft.dataType)) {
      if (!addFieldDraft.referenceDirectoryTypeId) {
        setAddError('Для ссылочного поля выберите справочник')
        return
      }
    }

    if (requiresEnumValues(addFieldDraft.dataType)) {
      const hasValues = addFieldDraft.enumValues
        .split(',')
        .map((value) => value.trim())
        .filter(Boolean).length
      if (!hasValues) {
        setAddError('Заполните список значений Enum')
        return
      }
    }

    const payload = {
      name: addFieldDraft.name.trim(),
      columnName: addFieldDraft.columnName.trim(),
      dataType: addFieldDraft.dataType,
      isRequired: false,
      order: fields.length + 1,
      isCollection: addFieldDraft.isCollection,
      referenceDirectoryTypeId:
        addFieldDraft.referenceDirectoryTypeId || null,
      enumValues: requiresEnumValues(addFieldDraft.dataType)
        ? addFieldDraft.enumValues
            .split(',')
            .map((value) => value.trim())
            .filter(Boolean)
        : null,
    }

    try {
      setAdding(true)
      const created = await apiRequest(
        `/api/directory-types/${type.id}/fields`,
        {
          method: 'POST',
          body: payload,
          apiKey,
        },
      )
      setFields((prev) => [...prev, normalizeFieldShape(created)])
      await onUpdated?.()
      resetAddFieldDraft()
    } catch (requestError) {
      setAddError(requestError.message)
    } finally {
      setAdding(false)
    }
  }

  const startEditField = (field) => {
    setEditingFieldId(field.id)
    setEditDraft({
      name: field.name,
      order: field.order ?? 1,
      isRequired: field.isRequired,
    })
    setError('')
  }

  const handleEditDraftChange = (event) => {
    const { name, value, type: inputType, checked } = event.target
    setEditDraft((prev) => ({
      ...prev,
      [name]:
        inputType === 'checkbox'
          ? checked
          : name === 'order'
            ? Number(value)
            : value,
    }))
  }

  const handleUpdateField = async (event) => {
    event.preventDefault()
    if (!editingFieldId) return
    if (!editDraft.name.trim()) {
      setError('Название обязательно')
      return
    }

    const payload = {
      name: editDraft.name.trim(),
      order: editDraft.order,
      isRequired: editDraft.isRequired,
    }

    try {
      setEditing(true)
      const updated = await apiRequest(
        `/api/directory-types/${type.id}/fields/${editingFieldId}`,
        {
          method: 'PUT',
          body: payload,
          apiKey,
        },
      )
      setFields((prev) =>
        prev.map((field) => {
          const updatedId = updated.id ?? updated.Id
          return field.id === updatedId ? normalizeFieldShape(updated) : field
        }),
      )
      await onUpdated?.()
      setEditingFieldId(null)
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setEditing(false)
    }
  }

  const handleDeleteField = async (fieldId) => {
    const confirmed = window.confirm(
      'При удалении поля данные из всех записей будут потеряны. Продолжить?',
    )
    if (!confirmed) return

    try {
      await apiRequest(
        `/api/directory-types/${type.id}/fields/${fieldId}`,
        {
          method: 'DELETE',
          apiKey,
        },
      )
      setFields((prev) => prev.filter((field) => field.id !== fieldId))
      await onUpdated?.()
      if (editingFieldId === fieldId) {
        setEditingFieldId(null)
      }
    } catch (requestError) {
      setError(requestError.message)
    }
  }

  return (
    <section className="panel card">
      <header className="panel__header">
        <div>
          <h2>Настройка полей: {type.name}</h2>
          <p>Управляйте столбцами справочника и добавляйте новые.</p>
        </div>
        <div className="panel__actions">
          <button type="button" className="ghost-button" onClick={onBack}>
            Назад
          </button>
        </div>
      </header>

      {error && <div className="panel__error">{error}</div>}

      <div className="manager__settings">
        <h3>Настройка типа</h3>
        <form className="inline-form" onSubmit={handleGroupSubmit}>
          <label>
            Группа доступов
            <select
              value={groupDraft}
              onChange={(event) => setGroupDraft(event.target.value)}
              disabled={groupSaving}
            >
              <option value="">Без группы</option>
              {groups.map((group) => (
                <option key={group.id} value={group.id}>
                  {group.name}
                </option>
              ))}
            </select>
          </label>
          {groupError && <div className="panel__error">{groupError}</div>}
          {groupSuccess && <p className="manager__hint">{groupSuccess}</p>}
          <button
            type="submit"
            className="secondary-button"
            disabled={!isGroupDirty || groupSaving}
          >
            {groupSaving ? 'Сохранение...' : 'Сохранить'}
          </button>
        </form>
      </div>

      <div className="manager">
        <div className="manager__list">
          <h3>Существующие поля</h3>
          <table>
            <thead>
              <tr>
                <th>Порядок</th>
                <th>Название</th>
                <th>Колонка</th>
                <th>Тип</th>
                <th>Обязательное</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {sortedFields.map((field) => (
                <tr
                  key={field.id}
                  className={
                    editingFieldId === field.id
                      ? 'manager__row manager__row--active'
                      : 'manager__row'
                  }
                >
                  <td>{field.order}</td>
                  <td>{field.name}</td>
                  <td>{field.columnName}</td>
                  <td>{getFieldTypeLabel(field.dataType)}</td>
                  <td>{field.isRequired ? 'Да' : 'Нет'}</td>
                  <td>
                    <div className="table-actions">
                      <button
                        type="button"
                        className="ghost-button manager__table-button"
                        onClick={() => startEditField(field)}
                      >
                        Изменить
                      </button>
                      <button
                        type="button"
                        className="ghost-button manager__table-button manager__table-button--danger"
                        onClick={() => handleDeleteField(field.id)}
                      >
                        Удалить
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {editingFieldId && (
            <form className="inline-form" onSubmit={handleUpdateField}>
              <h4>Редактирование поля</h4>
              <label>
                Название
                <input
                  type="text"
                  name="name"
                  value={editDraft.name}
                  onChange={handleEditDraftChange}
                  required
                />
              </label>

              <label>
                Порядок
                <input
                  type="number"
                  name="order"
                  min="1"
                  value={editDraft.order}
                  onChange={handleEditDraftChange}
                />
              </label>

              <label className="inline-form__checkbox">
                <input
                  type="checkbox"
                  name="isRequired"
                  checked={editDraft.isRequired}
                  onChange={handleEditDraftChange}
                />
                Обязательное
              </label>

              <div className="inline-form__actions">
                <button type="submit" className="secondary-button" disabled={editing}>
                  Сохранить
                </button>
                <button
                  type="button"
                  className="ghost-button"
                  onClick={() => setEditingFieldId(null)}
                >
                  Отмена
                </button>
              </div>
            </form>
          )}
        </div>

        <div className="manager__form">
          <h3>Новый столбец</h3>
          <p className="manager__hint">
            Новый столбец не может быть обязательным до заполнения данных.
          </p>
          {addError && <div className="panel__error">{addError}</div>}
          <form onSubmit={handleAddField}>
            <label>
              Название
              <input
                type="text"
                name="name"
                value={addFieldDraft.name}
                onChange={handleAddDraftChange}
                required
              />
            </label>
            <label>
              Колонка
              <input
                type="text"
                name="columnName"
                value={addFieldDraft.columnName}
                onChange={handleAddDraftChange}
                required
              />
            </label>
            <label>
              Тип
              <select
                name="dataType"
                value={addFieldDraft.dataType}
                onChange={handleAddDraftChange}
              >
                {FIELD_DATA_TYPES.map((typeOption) => (
                  <option key={typeOption.value} value={typeOption.value}>
                    {typeOption.label}
                  </option>
                ))}
              </select>
            </label>

            {requiresReferenceDirectory(addFieldDraft.dataType) && (
              <label>
                Связанный справочник
                <select
                  name="referenceDirectoryTypeId"
                  value={addFieldDraft.referenceDirectoryTypeId}
                  onChange={handleAddDraftChange}
                >
                  <option value="">Не выбрано</option>
                  {directoryTypes
                    .filter((directory) => directory.id !== type.id)
                    .map((directory) => (
                      <option key={directory.id} value={directory.id}>
                        {directory.name}
                      </option>
                    ))}
                </select>
              </label>
            )}

            {requiresEnumValues(addFieldDraft.dataType) && (
              <label>
                Значения Enum
                <input
                  type="text"
                  name="enumValues"
                  value={addFieldDraft.enumValues}
                  onChange={handleAddDraftChange}
                  placeholder="Первый, Второй, ..."
                />
              </label>
            )}

            <label className="inline-form__checkbox">
              <input
                type="checkbox"
                name="isCollection"
                checked={addFieldDraft.isCollection}
                onChange={handleAddDraftChange}
              />
              Множественное значение
            </label>

            <button
              type="submit"
              className="secondary-button"
              disabled={adding}
            >
              Добавить столбец
            </button>
          </form>
        </div>
      </div>
    </section>
  )
}

const normalizeFieldShape = (field) => ({
  id: field.id ?? field.Id,
  name: field.name ?? field.Name,
  columnName: field.columnName ?? field.ColumnName,
  dataType: field.dataType ?? field.DataType,
  isRequired: field.isRequired ?? field.IsRequired,
  order: field.order ?? field.Order ?? 0,
  isCollection: field.isCollection ?? field.IsCollection,
  referenceDirectoryTypeId:
    field.referenceDirectoryTypeId ?? field.ReferenceDirectoryTypeId ?? null,
  enumValues: field.enumValues ?? field.EnumValues ?? null,
})