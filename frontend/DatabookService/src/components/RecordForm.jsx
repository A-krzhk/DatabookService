import { useEffect, useMemo, useState } from 'react'
import { apiRequest } from '../api/httpClient'
import { getFieldTypeLabel } from '../constants/fieldDataTypes'
import {
  buildInitialValues,
  buildPayload,
  getFieldKey,
} from '../utils/recordFieldUtils'
import { RecordFieldControl } from './RecordFieldControl'

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

  const handleValueChange = (field, nextValue) => {
    const key = getFieldKey(field)
    setValues((prev) => ({
      ...prev,
      [key]: nextValue,
    }))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    try {
      setSaving(true)

      const payload = {
        tableId: type.id,
        fieldsValues: buildPayload(sortedFields, values),
      }

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
          <h2>Создание записи · {type.name}</h2>
          <p>Заполните значения во всех обязательных полях, чтобы сохранить запись.</p>
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
        {sortedFields.map((field) => {
          const key = getFieldKey(field)
          return (
            <div key={field.id} className="record-form__field">
              <label>
                <span>
                  {field.name}{' '}
                  {!field.isRequired && <span className="muted">(необязательно)</span>}
                </span>
                <RecordFieldControl
                  apiKey={apiKey}
                  field={field}
                  value={values[key]}
                  onChange={(nextValue) => handleValueChange(field, nextValue)}
                />
              </label>
              <small>
                {getFieldTypeLabel(field.dataType)}
                {field.isCollection ? ' · коллекция' : ''}
              </small>
            </div>
          )
        })}
      </form>
    </section>
  )
}
