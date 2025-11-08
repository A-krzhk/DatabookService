import { useEffect, useMemo, useRef, useState } from 'react'
import { apiRequest } from '../api/httpClient'
import { RecordFieldControl } from './RecordFieldControl'
import {
  buildInitialValues,
  buildPayload,
  getFieldKey,
} from '../utils/recordFieldUtils'
import { getFieldTypeLabel } from '../constants/fieldDataTypes'

export function RecordDetail({
  apiKey,
  type,
  recordId,
  onBack,
  onDeleted,
}) {
  const sortedFields = useMemo(
    () => [...(type.fields ?? [])].sort((a, b) => (a.order ?? 0) - (b.order ?? 0)),
    [type.fields],
  )

  const [recordData, setRecordData] = useState(null)
  const [values, setValues] = useState(() => buildInitialValues(sortedFields))
  const initialValuesRef = useRef(buildInitialValues(sortedFields))
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [saving, setSaving] = useState(false)
  const [tab, setTab] = useState('main')
  const [history, setHistory] = useState([])
  const [historyPagination, setHistoryPagination] = useState(null)
  const [historyLoading, setHistoryLoading] = useState(false)
  const [historyError, setHistoryError] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    const loadRecord = async () => {
      try {
        setLoading(true)
        const response = await apiRequest(
          `/api/directory-record/${type.id}/${recordId}`,
          { apiKey, signal: controller.signal },
        )
        const payload = response?.data ?? response?.Data ?? {}
        setRecordData(payload)
      } catch (requestError) {
        if (!controller.signal.aborted) {
          setError(requestError.message)
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      }
    }
    loadRecord()
    return () => controller.abort()
  }, [apiKey, recordId, type.id])

  useEffect(() => {
    if (!recordData) return
    const nextValues = buildInitialValues(sortedFields, recordData)
    setValues(nextValues)
    initialValuesRef.current = nextValues
    setSuccess('')
  }, [sortedFields, recordData])

  useEffect(() => {
    setTab('main')
    setHistory([])
    setHistoryPagination(null)
    setHistoryError('')
    setSuccess('')
    setError('')
  }, [recordId])

  useEffect(() => {
    if (tab !== 'history' || history.length) return
    const controller = new AbortController()
    const loadHistory = async () => {
      try {
        setHistoryLoading(true)
        const response = await apiRequest(
          `/api/history/directory-type/${type.id}`,
          {
            apiKey,
            signal: controller.signal,
            searchParams: {
              pageNumber: 1,
              pageSize: 50,
              recordId,
            },
          },
        )
        setHistory(response?.records ?? response?.Records ?? [])
        setHistoryPagination(response?.pagination ?? response?.Pagination ?? null)
      } catch (requestError) {
        if (!controller.signal.aborted) {
          setHistoryError(requestError.message)
        }
      } finally {
        if (!controller.signal.aborted) {
          setHistoryLoading(false)
        }
      }
    }
    loadHistory()
    return () => controller.abort()
  }, [apiKey, recordId, tab, type.id, history.length])

  const handleValueChange = (field, nextValue) => {
    const key = getFieldKey(field)
    setValues((prev) => ({
      ...prev,
      [key]: nextValue,
    }))
    setSuccess('')
  }

  const isDirty =
    JSON.stringify(values) !== JSON.stringify(initialValuesRef.current)

  const handleSave = async () => {
    setError('')
    setSuccess('')
    try {
      setSaving(true)
      const payload = buildPayload(sortedFields, values)
      const response = await apiRequest(
        `/api/directory-record/${type.id}/${recordId}`,
        {
          method: 'PUT',
          apiKey,
          body: { fieldsValues: payload },
        },
      )
      const updated = response ?? recordData
      setRecordData(updated)
      const nextValues = buildInitialValues(sortedFields, updated ?? {})
      setValues(nextValues)
      initialValuesRef.current = nextValues
      setSuccess('Изменения сохранены')
      refreshHistoryOnUpdate()
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setSaving(false)
    }
  }

  const refreshHistoryOnUpdate = () => {
    if (tab === 'history') {
      setHistory([])
      setHistoryPagination(null)
    }
  }

  const handleDelete = async () => {
    const confirmed = window.confirm(
      'Удалить запись? Вы всегда сможете восстановить её из списка удалённых.',
    )
    if (!confirmed) return
    try {
      await apiRequest(`/api/directory-records/${type.id}/${recordId}`, {
        method: 'DELETE',
        apiKey,
      })
      onDeleted?.()
    } catch (requestError) {
      setError(requestError.message)
    }
  }

  const data = recordData ?? {}

  return (
    <section className="panel card">
      <div className="record-detail__actions-bar">
        <button type="button" className="ghost-button" onClick={onBack}>
          Назад
        </button>
        <div className="record-detail__action-group">
          <button
            type="button"
            className="ghost-button"
            onClick={handleSave}
            disabled={!isDirty || saving}
          >
            Сохранить
          </button>
          <button
            type="button"
            className="danger-button"
            onClick={handleDelete}
          >
            Удалить
          </button>
        </div>
      </div>

      <div className="record-detail__tabs">
        <button
          type="button"
          className={tab === 'main' ? 'tab tab--active' : 'tab'}
          onClick={() => setTab('main')}
        >
          Основное
        </button>
        <button
          type="button"
          className={tab === 'history' ? 'tab tab--active' : 'tab'}
          onClick={() => setTab('history')}
        >
          История
        </button>
      </div>

      {tab === 'main' && success && <div className="panel__success">{success}</div>}
      {tab === 'main' && error && <div className="panel__error">{error}</div>}

      {tab === 'main' && (
        <div className="record-detail__content">
          {loading ? (
            <p>Загрузка записи...</p>
          ) : (
            <form className="record-form">
              {sortedFields.map((field) => {
                const key = getFieldKey(field)
                return (
                  <div key={field.id} className="record-form__field">
                    <label>
                      <span>
                        {field.name}{' '}
                        {!field.isRequired && (
                          <span className="muted">(необязательно)</span>
                        )}
                      </span>
                      <RecordFieldControl
                        apiKey={apiKey}
                        field={field}
                        value={values[key]}
                        onChange={(nextValue) =>
                          handleValueChange(field, nextValue)
                        }
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
          )}
        </div>
      )}

      {tab === 'history' && (
        <div className="record-detail__content">
          {historyError && <div className="panel__error">{historyError}</div>}
          {historyLoading ? (
            <p>Загружаем историю...</p>
          ) : (
            <div className="records-table__scroll record-detail__history">
              <table>
                <thead>
                  <tr>
                    <th>Дата</th>
                    <th>Поле</th>
                    <th>Было</th>
                    <th>Стало</th>
                    <th>Действие</th>
                    <th>Пользователь</th>
                  </tr>
                </thead>
                <tbody>
                  {!history.length && (
                    <tr>
                      <td colSpan={6}>История пуста</td>
                    </tr>
                  )}
          {history
            .filter((entry) => {
              const entryId = entry.recordId ?? entry.RecordId
              if (!recordId) return true
              if (!entryId) return true
              return String(entryId).toLowerCase() ===
                String(recordId).toLowerCase()
            })
            .map((entry) => (
              <tr key={entry.id ?? entry.Id}>
                <td>{formatValue(entry.changedAt ?? entry.ChangedAt)}</td>
                <td>{entry.fieldName ?? entry.FieldName}</td>
                <td>{formatValue(entry.oldValue ?? entry.OldValue)}</td>
                <td>{formatValue(entry.newValue ?? entry.NewValue)}</td>
                <td>{getActionLabel(entry)}</td>
                <td>{entry.changedBy ?? entry.ChangedBy}</td>
              </tr>
            ))}
                </tbody>
              </table>
            </div>
          )}
          {historyPagination && (
            <p className="muted">
              Показано {history.length} записей из{' '}
              {historyPagination.totalCount ?? historyPagination.TotalCount ?? '...'}
            </p>
          )}
        </div>
      )}
    </section>
  )
}

const ACTION_LABELS = {
  1: 'Создание',
  2: 'Обновление',
  3: 'Удаление',
  4: 'Чтение',
}

const getActionLabel = (entry) => {
  const raw = entry.action ?? entry.Action
  if (raw === undefined || raw === null) return '—'
  const numeric = Number(raw)
  if (!Number.isNaN(numeric) && ACTION_LABELS[numeric]) {
    return ACTION_LABELS[numeric]
  }
  return raw
}

const formatValue = (value) => {
  if (value === undefined || value === null || value === '') {
    return '—'
  }
  if (Array.isArray(value)) {
    return value.join(', ')
  }
  if (typeof value === 'boolean') {
    return value ? 'Да' : 'Нет'
  }
  if (value instanceof Date) {
    return value.toLocaleString()
  }
  if (
    typeof value === 'string' &&
    !Number.isNaN(Date.parse(value)) &&
    value.length > 5
  ) {
    const date = new Date(value)
    if (!Number.isNaN(date.getTime())) {
      return date.toLocaleString()
    }
  }
  return String(value)
}
