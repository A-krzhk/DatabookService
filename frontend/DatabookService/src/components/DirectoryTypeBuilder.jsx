import { useMemo, useState } from 'react'
import { apiRequest } from '../api/httpClient'
import {
  FIELD_DATA_TYPES,
  FieldDataType,
  getFieldTypeLabel,
  requiresEnumValues,
  requiresReferenceDirectory,
} from '../constants/fieldDataTypes'

const createFieldDraft = () => ({
  name: '',
  columnName: '',
  dataType: FieldDataType.STRING,
  isRequired: true,
  isCollection: false,
  referenceDirectoryTypeId: '',
  enumValues: '',
})

export function DirectoryTypeBuilder({
  apiKey,
  existingTypes,
  onCreated,
  onCancel,
  onRefreshTypes,
  groups = [],
}) {
  const [details, setDetails] = useState({
    name: '',
    tableName: '',
    description: '',
  })

  const [fields, setFields] = useState([])
  const [fieldDraft, setFieldDraft] = useState(createFieldDraft())
  const [editingIndex, setEditingIndex] = useState(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [fieldError, setFieldError] = useState('')
  const [groupId, setGroupId] = useState('')

  const tableNameSuggestion = useMemo(() => {
    return details.name
      .trim()
      .toLowerCase()
      .replace(/\s+/g, '_')
      .replace(/[^a-z0-9_]/g, '')
  }, [details.name])

  const handleFieldDraftChange = (event) => {
    const { name, value, type, checked } = event.target
    setFieldDraft((prev) => ({
      ...prev,
      [name]:
        type === 'checkbox'
          ? checked
          : name === 'dataType'
            ? Number(value)
            : value,
    }))
  }

  const resetFieldDraft = () => {
    setFieldDraft(createFieldDraft())
    setEditingIndex(null)
    setFieldError('')
  }

  const handleEditField = (index) => {
    const field = fields[index]
    setFieldDraft({
      name: field.name,
      columnName: field.columnName,
      dataType: field.dataType,
      isRequired: field.isRequired,
      isCollection: field.isCollection,
      referenceDirectoryTypeId: field.referenceDirectoryTypeId ?? '',
      enumValues: (field.enumValues ?? []).join(', '),
    })
    setEditingIndex(index)
  }

  const handleMoveField = (index, direction) => {
    const nextIndex = index + direction
    if (nextIndex < 0 || nextIndex >= fields.length) return
    const updated = [...fields]
    const temp = updated[index]
    updated[index] = updated[nextIndex]
    updated[nextIndex] = temp
    setFields(updated)
  }

  const handleRemoveField = (index) => {
    setFields((prev) => prev.filter((_, idx) => idx !== index))
    if (editingIndex === index) {
      resetFieldDraft()
    }
  }

  const handleFieldSubmit = (event) => {
    event.preventDefault()

    if (!fieldDraft.name.trim() || !fieldDraft.columnName.trim()) {
      setFieldError('Название и имя колонки обязательны')
      return
    }

    if (requiresReferenceDirectory(fieldDraft.dataType)) {
      if (!fieldDraft.referenceDirectoryTypeId) {
        setFieldError('Для ссылочного поля выберите справочник')
        return
      }
    }

    if (requiresEnumValues(fieldDraft.dataType)) {
      const hasValues = fieldDraft.enumValues
        .split(',')
        .map((value) => value.trim())
        .filter(Boolean).length
      if (!hasValues) {
        setFieldError('Перечислите значения для Enum через запятую')
        return
      }
    }

    const normalized = {
      name: fieldDraft.name.trim(),
      columnName: fieldDraft.columnName.trim(),
      dataType: fieldDraft.dataType,
      isRequired: fieldDraft.isRequired,
      isCollection: fieldDraft.isCollection,
      referenceDirectoryTypeId:
        fieldDraft.referenceDirectoryTypeId || null,
      enumValues: fieldDraft.enumValues
        ? fieldDraft.enumValues
            .split(',')
            .map((value) => value.trim())
            .filter(Boolean)
        : null,
    }

    setFields((prev) => {
      const next = [...prev]
      if (editingIndex !== null) {
        next[editingIndex] = { ...next[editingIndex], ...normalized }
      } else {
        next.push(normalized)
      }
      return next
    })

    resetFieldDraft()
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')

    if (!details.name.trim() || !details.tableName.trim()) {
      setError('Название и имя таблицы обязательны')
      return
    }

    if (!fields.length) {
      setError('Добавьте хотя бы одно поле')
      return
    }

    const payload = {
      name: details.name.trim(),
      tableName: details.tableName.trim(),
      description: details.description?.trim() || null,
      directoryGroupId: groupId || null,
      fields: fields.map((field, index) => ({
        ...field,
        order: index + 1,
      })),
    }

    try {
      setSaving(true)
      const result = await apiRequest('/api/directory-types', {
        method: 'POST',
        body: payload,
        apiKey,
      })
      onCreated?.(result)
      await onRefreshTypes?.()
      resetFieldDraft()
      setFields([])
      setDetails({
        name: '',
        tableName: '',
        description: '',
      })
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="panel card">
      <header className="panel__header">
        <div>
          <h2>Создание типа справочника</h2>
          <p>
            Добавьте общую информацию и сконструируйте поля. Перетаскивайте
            их в нужном порядке с помощью кнопок.
          </p>
        </div>
        <div className="panel__actions">
          <button type="button" className="ghost-button" onClick={onCancel}>
            Назад
          </button>
          <button
            type="button"
            className="ghost-button"
            onClick={() =>
              setDetails((prev) => ({
                ...prev,
                tableName: prev.tableName || tableNameSuggestion,
              }))
            }
          >
            Подставить имя таблицы
          </button>
          <button
            type="button"
            className="secondary-button"
            onClick={handleSubmit}
            disabled={saving}
          >
            Сохранить
          </button>
        </div>
      </header>

      {error && <div className="panel__error">{error}</div>}

      <div className="builder">
        <div className="builder__preview">
          <h3>Поля ({fields.length})</h3>
          <ul>
            {fields.map((field, index) => (
              <li key={`${field.columnName}-${index}`}>
                <div>
                  <span className="builder__field-name">
                    {index + 1}. {field.name}
                  </span>
                  <span className="builder__field-meta">
                    {field.columnName} · {getFieldTypeLabel(field.dataType)}
                  </span>
                </div>
                <div className="builder__field-actions">
                  <button
                    type="button"
                    onClick={() => handleMoveField(index, -1)}
                    disabled={index === 0}
                  >
                    ↑
                  </button>
                  <button
                    type="button"
                    onClick={() => handleMoveField(index, 1)}
                    disabled={index === fields.length - 1}
                  >
                    ↓
                  </button>
                  <button type="button" onClick={() => handleEditField(index)}>
                    Изменить
                  </button>
                  <button
                    type="button"
                    onClick={() => handleRemoveField(index)}
                  >
                    Удалить
                  </button>
                </div>
              </li>
            ))}
          </ul>
          {!fields.length && (
            <p className="builder__hint">
              Список пуст. Добавьте поле в форме справа.
            </p>
          )}
        </div>

        <div className="builder__form">
          <form onSubmit={handleSubmit}>
            <div className="form-grid">
              <label>
                Название
                <input
                  type="text"
                  value={details.name}
                  onChange={(event) =>
                    setDetails((prev) => ({ ...prev, name: event.target.value }))
                  }
                  placeholder="Например, Контрагенты"
                />
              </label>
            <label>
              Имя таблицы
              <input
                type="text"
                value={details.tableName}
                onChange={(event) =>
                  setDetails((prev) => ({
                    ...prev,
                    tableName: event.target.value,
                  }))
                }
                placeholder="contractors"
              />
            </label>
            <label>
              Группа
              <select
                value={groupId}
                onChange={(event) => setGroupId(event.target.value)}
              >
                <option value="">Без группы</option>
                {groups.map((group) => (
                  <option key={group.id} value={group.id}>
                    {group.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="form-grid__full">
              Описание
              <textarea
                  rows={3}
                  value={details.description}
                  onChange={(event) =>
                    setDetails((prev) => ({
                      ...prev,
                      description: event.target.value,
                    }))
                  }
                  placeholder="Кратко опишите назначение"
                />
              </label>
            </div>
          </form>

          <hr />

          <form onSubmit={handleFieldSubmit} className="field-form">
            <div className="field-form__header">
              <h3>{editingIndex !== null ? 'Редактирование поля' : 'Новое поле'}</h3>
              {editingIndex !== null && (
                <button
                  type="button"
                  className="ghost-button"
                  onClick={resetFieldDraft}
                >
                  Очистить
                </button>
              )}
            </div>

            {fieldError && <div className="panel__error">{fieldError}</div>}

            <label>
              Название
              <input
                type="text"
                name="name"
                value={fieldDraft.name}
                onChange={handleFieldDraftChange}
                required
              />
            </label>

            <label>
              Имя в базе
              <input
                type="text"
                name="columnName"
                value={fieldDraft.columnName}
                onChange={handleFieldDraftChange}
                required
              />
            </label>

            <label>
              Тип данных
              <select
                name="dataType"
                value={fieldDraft.dataType}
                onChange={handleFieldDraftChange}
              >
                {FIELD_DATA_TYPES.map((type) => (
                  <option key={type.value} value={type.value}>
                    {type.label}
                  </option>
                ))}
              </select>
            </label>

            {requiresReferenceDirectory(fieldDraft.dataType) && (
              <label>
                Справочник для связи
                <select
                  name="referenceDirectoryTypeId"
                  value={fieldDraft.referenceDirectoryTypeId}
                  onChange={handleFieldDraftChange}
                >
                  <option value="">Не выбрано</option>
                  {existingTypes.map((type) => (
                    <option key={type.id} value={type.id}>
                      {type.name}
                    </option>
                  ))}
                </select>
              </label>
            )}

            {requiresEnumValues(fieldDraft.dataType) && (
              <label>
                Значения (через запятую)
                <input
                  type="text"
                  name="enumValues"
                  value={fieldDraft.enumValues}
                  onChange={handleFieldDraftChange}
                  placeholder="Новичок, Постоянный, VIP"
                />
              </label>
            )}

            <div className="field-form__toggles">
              <label>
                <input
                  type="checkbox"
                  name="isRequired"
                  checked={fieldDraft.isRequired}
                  onChange={handleFieldDraftChange}
                />
                Обязательное
              </label>
              <label>
                <input
                  type="checkbox"
                  name="isCollection"
                  checked={fieldDraft.isCollection}
                  onChange={handleFieldDraftChange}
                />
                Множественное
              </label>
            </div>

            <div className="field-form__actions">
              <button type="submit" className="secondary-button">
                {editingIndex !== null ? 'Обновить поле' : 'Добавить поле'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </section>
  )
}
