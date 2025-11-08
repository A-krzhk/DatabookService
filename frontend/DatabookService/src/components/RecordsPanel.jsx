import { useEffect, useState } from 'react'
import { apiRequest } from '../api/httpClient'
import { Pagination } from './Pagination'

const PAGE_SIZE = 20

const getRecordId = (record) =>
  record?.Id ??
  record?.id ??
  record?.ID ??
  record?.RecordId ??
  record?.recordId ??
  null

export function RecordsPanel({
  apiKey,
  type,
  onOpenRecord,
  onCreateRecord,
  onRefresh,
}) {
  const [mode, setMode] = useState('active')
  const [page, setPage] = useState(1)
  const [rows, setRows] = useState([])
  const [columns, setColumns] = useState([])
  const [pagination, setPagination] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [version, setVersion] = useState(0)

  useEffect(() => {
    setPage(1)
    setMode('active')
  }, [type.id])

  useEffect(() => {
    setPage(1)
  }, [mode])

  useEffect(() => {
    const controller = new AbortController()
    const load = async () => {
      if (!type?.id) return
      setLoading(true)
      setError('')
      try {
        const endpoint =
          mode === 'active'
            ? `/api/directory-record/${type.id}/all`
            : `/api/directory-record/${type.id}/deleted`

        const response = await apiRequest(endpoint, {
          apiKey,
          signal: controller.signal,
          searchParams: { page, size: PAGE_SIZE },
        })

        const normalizedColumns = response?.columns ?? response?.Columns ?? []
        const normalizedRows = response?.data ?? response?.Data ?? []
        const normalizedPagination =
          response?.pagination ?? response?.Pagination ?? null

        setColumns(normalizedColumns)
        setRows(Array.isArray(normalizedRows) ? normalizedRows : [])
        setPagination(normalizedPagination)
      } catch (requestError) {
        if (!controller.signal.aborted) {
          setError(requestError.message)
          setRows([])
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      }
    }
    load()
    return () => controller.abort()
  }, [apiKey, mode, page, type?.id, version])

  const refresh = () => setVersion((prev) => prev + 1)

  const handleRestore = async (recordId) => {
    try {
      await apiRequest(
        `/api/directory-record/${type.id}/restore/${recordId}`,
        {
          method: 'POST',
          apiKey,
        },
      )
      refresh()
      await onRefresh?.()
    } catch (requestError) {
      setError(requestError.message)
    }
  }

  const handleDeleteToggle = () => {
    setMode((prev) => (prev === 'active' ? 'deleted' : 'active'))
    setPage(1)
  }

  const handlePagination = (nextPage) => {
    if (nextPage < 1) return
    if (pagination && nextPage > (pagination.totalPages ?? pagination.TotalPages ?? 1)) {
      return
    }
    setPage(nextPage)
  }

  const hasData = rows.length > 0

  return (
    <section className="panel card">
      <header className="panel__header">
        <div>
          <h2>{type.name}</h2>
          <p>Таблица: {type.tableName}</p>
        </div>
        <div className="panel__actions">
          <button type="button" className="secondary-button" onClick={onCreateRecord}>
            Создать
          </button>
          <button type="button" className="ghost-button">
            Импорт CSV
          </button>
          <button type="button" className="ghost-button">
            Экспорт CSV
          </button>
          <button
            type="button"
            className={
              mode === 'deleted'
                ? 'secondary-button secondary-button--warning'
                : 'ghost-button'
            }
            onClick={handleDeleteToggle}
          >
            {mode === 'deleted' ? 'Показать актуальные' : 'Удаленные'}
          </button>
        </div>
      </header>

      {error && <div className="panel__error">{error}</div>}

      <div className="records-table">
        <div className="records-table__header">
          <span>
            {mode === 'deleted' ? 'Удаленные записи' : 'Все записи'} · страница {page}
          </span>
          {loading && <span className="sidebar__status">Загрузка...</span>}
        </div>
        <div className="records-table__scroll">
          <table>
            <thead>
              <tr>
                {columns.map((column) => (
                  <th key={column.fieldName ?? column.FieldName}>
                    {column.displayName ?? column.DisplayName}
                  </th>
                ))}
                <th>{mode === 'deleted' ? 'Действия' : ''}</th>
              </tr>
            </thead>
            <tbody>
              {!hasData && (
                <tr>
                  <td colSpan={columns.length + 1}>
                    {mode === 'deleted'
                      ? 'Нет удаленных записей'
                      : 'Записи отсутствуют'}
                  </td>
                </tr>
              )}
              {rows.map((row, index) => {
                const recordId = getRecordId(row)
                return (
                  <tr
                    key={recordId ?? `row-${index}`}
                    className={
                      mode === 'deleted' ? 'records-table__row' : 'records-table__row records-table__row--clickable'
                    }
                    onClick={() =>
                      mode === 'active' && recordId && onOpenRecord(recordId)
                    }
                  >
                    {columns.map((column) => {
                      const key = column.fieldName ?? column.FieldName
                      const value = row[key]
                      return <td key={`${recordId ?? index}-${key}`}>{renderCell(value)}</td>
                    })}
                    <td>
                      {mode === 'deleted' && recordId && (
                        <button
                          type="button"
                          className="secondary-button"
                          onClick={(event) => {
                            event.stopPropagation()
                            handleRestore(recordId)
                          }}
                        >
                          Восстановить
                        </button>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>

        {pagination && pagination.totalPages > 1 && (
          <Pagination pagination={normalizePagination(pagination)} onChange={handlePagination} />
        )}
      </div>
    </section>
  )
}

const renderCell = (value) => {
  if (value === null || value === undefined) return '—'
  if (Array.isArray(value)) return value.join(', ')
  if (typeof value === 'object') return JSON.stringify(value)
  if (typeof value === 'boolean') return value ? 'Да' : 'Нет'
  return String(value)
}

const normalizePagination = (pagination) => ({
  pageNumber: pagination.pageNumber ?? pagination.PageNumber ?? 1,
  pageSize: pagination.pageSize ?? pagination.PageSize ?? PAGE_SIZE,
  totalCount: pagination.totalCount ?? pagination.TotalCount ?? 0,
  totalPages: pagination.totalPages ?? pagination.TotalPages ?? 1,
  hasPrevious: pagination.hasPrevious ?? pagination.HasPrevious ?? false,
  hasNext: pagination.hasNext ?? pagination.HasNext ?? false,
})
