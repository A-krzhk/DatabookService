import { useEffect, useMemo, useState } from 'react'
import { apiRequest } from '../api/httpClient'
import { FieldDataType, getFieldTypeLabel } from '../constants/fieldDataTypes'

export function RecordForm({ apiKey, type, onCancel, onCreated }) {
  const sortedFields = useMemo(
    () => [...(type.fields ?? [])].sort((a, b) => (a.order ?? 0) - (b.order ?? 0)),
    [type.fields],
  )

  const [values, setValues] = useState(() => buildInitialValues(sortedFields))
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    setValues(buildInitialValues(sortedFields))
  }, [sortedFields])

  const handleChange = (field, event) => {
    if (field.dataType === FieldDataType.CHECKBOX && !field.isCollection) {
      setValues((prev) => ({
        ...prev,
        [field.columnName]: event.target.checked,
      }))
      return
    }

    if (field.isCollection && field.dataType === FieldDataType.ENUM) {
      const selected = Array.from(event.target.selectedOptions).map(
        (option) => option.value,
      )
      setValues((prev) => ({
        ...prev,
        [field.columnName]: selected,
      }))
      return
    }

    setValues((prev) => ({
      ...prev,
      [field.columnName]: event.target.value,
    }))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    try {
      setSaving(true)

      const payload = {
        tableId: type.id,
        fieldsValues: {},
      }

      sortedFields.forEach((field) => {
        const value = values[field.columnName]
        if (value === '' || value === null || value === undefined) return
        payload.fieldsValues[field.columnName] = normalizeValue(field, value)
      })

      await apiRequest('/api/directory-record', {
        method: 'POST',
        apiKey,
        body: payload,
      })

      onCreated?.()
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
          <h2>Новая запись · {type.name}</h2>
          <p>Заполните данные и нажмите «Сохранить», чтобы добавить запись.</p>
        </div>
        <div className="panel__actions">
          <button type="button" className="ghost-button" onClick={onCancel}>
            Назад
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

      <form className="record-form" onSubmit={handleSubmit}>
        {sortedFields.map((field) => (
          <div key={field.id} className="record-form__field">
            <label>
              <span>
                {field.name}{' '}
                {!field.isRequired && <span className="muted">(необязательно)</span>}
              </span>
              {renderInput(field, values[field.columnName], (event) =>
                handleChange(field, event),
              )}
            </label>
            <small>
              {getFieldTypeLabel(field.dataType)}
              {field.isCollection ? ' · коллекция' : ''}
            </small>
          </div>
        ))}
      </form>
    </section>
  )
}

const normalizeValue = (field, value) => {
  if (field.isCollection) {
    if (Array.isArray(value)) return value
    return String(value)
      .split(/[\n,]/)
      .map((item) => item.trim())
      .filter(Boolean)
  }

  switch (field.dataType) {
    case FieldDataType.NUMBER:
      return Number(value)
    case FieldDataType.CHECKBOX:
      return Boolean(value)
    default:
      return value
  }
}

const renderInput = (field, value, onChange) => {
  if (field.dataType === FieldDataType.CHECKBOX && !field.isCollection) {
    return (
      <input
        type="checkbox"
        checked={Boolean(value)}
        onChange={onChange}
      />
    )
  }

  if (field.isCollection && field.dataType === FieldDataType.ENUM) {
    const options = field.enumValues ?? field.collectionData?.map((item) => item.value) ?? []
    return (
      <select multiple value={value} onChange={onChange}>
        {options.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
    )
  }

  if (field.dataType === FieldDataType.ENUM) {
    const options = field.enumValues ?? field.collectionData?.map((item) => item.value) ?? []
    return (
      <select value={value} onChange={onChange}>
        <option value="">Не выбрано</option>
        {options.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
    )
  }

  if (field.isCollection) {
    return (
      <textarea
        rows={3}
        value={Array.isArray(value) ? value.join(', ') : value}
        onChange={onChange}
        placeholder="Перечислите значения через запятую"
      />
    )
  }

  const inputType = resolveInputType(field.dataType)

  return <input type={inputType} value={value} onChange={onChange} />
}

const resolveInputType = (dataType) => {
  switch (dataType) {
    case FieldDataType.NUMBER:
      return 'number'
    case FieldDataType.DATE:
      return 'date'
    case FieldDataType.DATETIME:
      return 'datetime-local'
    default:
      return 'text'
  }
}

const buildInitialValues = (fields) =>
  fields.reduce((acc, field) => {
    acc[field.columnName] = field.isCollection ? [] : ''
    return acc
  }, {})
