import { useEffect, useMemo, useState } from 'react'
import { apiRequest } from '../api/httpClient'

export function RecordDetail({
  apiKey,
  type,
  recordId,
  onBack,
  onDeleted,
}) {
  const [record, setRecord] = useState(null)
  const [columns, setColumns] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
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
        setRecord(response?.data ?? response?.Data ?? {})
        setColumns(response?.columns ?? response?.Columns ?? [])
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

  useEffect(() => {
    setTab('main')
    setHistory([])
    setHistoryPagination(null)
    setHistoryError('')
  }, [recordId])

  const orderedColumns = useMemo(
    () => [...columns].sort((a, b) => (a.order ?? 0) - (b.order ?? 0)),
    [columns],
  )

  const handleDelete = async () => {
    const confirmed = window.confirm(
      'Удалить запись? Вы всегда сможете восстановить её из списка удаленных.',
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

  const data = record ?? {}

  return (
    <section className="panel card">
      <div className="record-detail__actions-bar">
        <button type="button" className="ghost-button" onClick={onBack}>
          Назад
        </button>
        <div className="record-detail__action-group">
          <button type="button" className="ghost-button">
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

      {tab === 'main' && (
        <div className="record-detail__content">
          {error && <div className="panel__error">{error}</div>}
          {loading ? (
            <p>Загрузка записи...</p>
          ) : (
            <dl className="record-detail__fields">
              {orderedColumns.map((column) => {
                const key = column.fieldName ?? column.FieldName
                const label = column.displayName ?? column.DisplayName ?? key
                const value = data[key]
                return (
                  <div key={key} className="record-detail__field">
                    <dt>{label}</dt>
                    <dd>{formatValue(value)}</dd>
                  </div>
                )
              })}
            </dl>
          )}
        </div>
      )}

      {tab === 'history' && (
        <div className="record-detail__content">
          {historyError && <div className="panel__error">{historyError}</div>}
          {historyLoading ? (
            <p>Загружаем историю...</p>
          ) : (
            <div className="records-table__scroll">
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
                        <td>{entry.action ?? entry.Action}</td>
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
