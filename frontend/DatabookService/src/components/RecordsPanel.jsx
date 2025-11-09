import { useEffect, useMemo, useRef, useState } from 'react'
import { apiRequest, buildApiUrl } from '../api/httpClient'
import { Pagination } from './Pagination'
import {
  fetchReferenceOptions,
  getCachedReferenceOptions,
} from '../utils/referenceOptions'

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
  const [notice, setNotice] = useState('')
  const [version, setVersion] = useState(0)
  const [importing, setImporting] = useState(false)
  const [exporting, setExporting] = useState(false)
  const [referenceLookups, setReferenceLookups] = useState({})
  const fileInputRef = useRef(null)
  const referenceColumns = useMemo(
    () =>
      columns
        .map((column) => {
          const key = column.fieldName ?? column.FieldName ?? null
          const reference = column.reference ?? column.Reference ?? null
          const directoryTypeId =
            reference?.directoryTypeId ?? reference?.DirectoryTypeId ?? null
          if (!key || !directoryTypeId) return null
          return {
            key,
            directoryTypeId,
            isCollection: column.isCollection ?? column.IsCollection ?? false,
          }
        })
        .filter(Boolean),
    [columns],
  )
  const referenceColumnsMap = useMemo(() => {
    const map = new Map()
    referenceColumns.forEach((column) => {
      map.set(column.key, column)
    })
    return map
  }, [referenceColumns])

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

  useEffect(() => {
    if (!apiKey?.trim() || !referenceColumns.length) {
      setReferenceLookups({})
      return
    }

    let cancelled = false
    const directoryIds = Array.from(
      new Set(referenceColumns.map((column) => column.directoryTypeId)),
    )

    const cachedEntries = directoryIds
      .map((directoryTypeId) => {
        const cached = getCachedReferenceOptions(apiKey, directoryTypeId)
        return cached
          ? [directoryTypeId, buildReferenceLookup(cached)]
          : null
      })
      .filter(Boolean)

    if (cachedEntries.length) {
      setReferenceLookups((prev) => mergeReferenceLookups(prev, cachedEntries))
    }

    const missingIds = directoryIds.filter(
      (directoryTypeId) =>
        !getCachedReferenceOptions(apiKey, directoryTypeId),
    )

    if (!missingIds.length) {
      return
    }

    const load = async () => {
      try {
        const fetchedEntries = await Promise.all(
          missingIds.map(async (directoryTypeId) => {
            const options = await fetchReferenceOptions(apiKey, directoryTypeId)
            return [directoryTypeId, buildReferenceLookup(options)]
          }),
        )
        if (!cancelled) {
          setReferenceLookups((prev) =>
            mergeReferenceLookups(prev, fetchedEntries),
          )
        }
      } catch {
        if (!cancelled) {
          // Ignore lookup errors and fallback to raw values
        }
      }
    }

    load()

    return () => {
      cancelled = true
    }
  }, [apiKey, referenceColumns])

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

  const handleImportClick = () => {
    fileInputRef.current?.click()
  }

  const handleImportFile = async (event) => {
    const file = event.target.files?.[0]
    if (!file) return

    setImporting(true)
    setError('')
    setNotice('')

    try
    {
      const formData = new FormData()
      formData.append('file', file)

      const result = await apiRequest(
        `/api/directory-record/${type.id}/import`,
        {
          method: 'POST',
          apiKey,
          body: formData,
        },
      )

      const imported = result?.imported ?? 0
      const skipped = result?.skipped ?? 0
      setNotice(`Импортировано ${imported}, пропущено ${skipped}`)

      if (Array.isArray(result?.errors) && result.errors.length) {
        setError(result.errors.slice(0, 5).join('; '))
      }

      refresh()
      await onRefresh?.()
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setImporting(false)
      if (event.target) event.target.value = ''
    }
  }

  const handleExport = async () => {
    setExporting(true)
    setError('')
    setNotice('')

    try {
      const url = buildApiUrl(`/api/directory-record/${type.id}/export`)
      const response = await fetch(url, {
        headers: {
          'X-API-Key': apiKey.trim(),
          Accept: 'text/csv',
        },
      })

      if (!response.ok) {
        throw new Error('Не удалось экспортировать данные')
      }

      const blob = await response.blob()
      const disposition = response.headers.get('content-disposition')
      const fileName = extractFileName(disposition) ?? `${type.tableName}.csv`

      const downloadUrl = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = downloadUrl
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(downloadUrl)

      setNotice('Экспорт завершён')
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setExporting(false)
    }
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
          <button
            type="button"
            className="ghost-button"
            onClick={handleImportClick}
            disabled={importing || !apiKey}
          >
            Импорт CSV
          </button>
          <button
            type="button"
            className="ghost-button"
            onClick={handleExport}
            disabled={exporting || !apiKey}
          >
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
            {mode === 'deleted' ? 'Показать актуальные' : 'Удалённые'}
          </button>
        </div>
        <input
          ref={fileInputRef}
          type="file"
          accept=".csv"
          style={{ display: 'none' }}
          onChange={handleImportFile}
        />
      </header>

      {notice && <div className="panel__success">{notice}</div>}
      {error && <div className="panel__error">{error}</div>}

      <div className="records-table">
        <div className="records-table__header">
          <span>
            {mode === 'deleted' ? 'Удалённые записи' : 'Все записи'} · страница {page}
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
                      ? 'Нет удалённых записей'
                      : 'Пока нет записей'}
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
                      const referenceInfo = key
                        ? referenceColumnsMap.get(key)
                        : null
                      const formattedValue = referenceInfo
                        ? formatReferenceValue(value, referenceInfo, referenceLookups)
                        : null
                      return (
                        <td key={`${recordId ?? index}-${key}`}>
                          {formattedValue ?? renderCell(value)}
                        </td>
                      )
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
  if (value instanceof Date) return formatDateValue(value)
  if (typeof value === 'string') {
    const parsed = Date.parse(value)
    if (!Number.isNaN(parsed)) {
      return formatDateValue(new Date(parsed))
    }
  }
  if (typeof value === 'object') return JSON.stringify(value)
  if (typeof value === 'boolean') return value ? 'Да' : 'Нет'
  return String(value)
}

const formatDateValue = (date) =>
  date.toLocaleString([], {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })

const normalizePagination = (pagination) => ({
  pageNumber: pagination.pageNumber ?? pagination.PageNumber ?? 1,
  pageSize: pagination.pageSize ?? pagination.PageSize ?? PAGE_SIZE,
  totalCount: pagination.totalCount ?? pagination.TotalCount ?? 0,
  totalPages: pagination.totalPages ?? pagination.TotalPages ?? 1,
  hasPrevious: pagination.hasPrevious ?? pagination.HasPrevious ?? false,
  hasNext: pagination.hasNext ?? pagination.HasNext ?? false,
})

const mergeReferenceLookups = (current, entries) => {
  const next = { ...current }
  entries.forEach(([directoryTypeId, lookup]) => {
    next[directoryTypeId] = lookup
  })
  return next
}

const buildReferenceLookup = (options = []) =>
  options.reduce((acc, option) => {
    const key = normalizeReferenceLookupKey(option?.id ?? option?.Id)
    if (key) {
      acc[key] = option?.label ?? option?.Label ?? ''
    }
    return acc
  }, {})

const normalizeReferenceLookupKey = (value) => {
  if (value === null || value === undefined) return ''
  if (typeof value === 'object') {
    if ('id' in value && value.id !== undefined) {
      return String(value.id).toLowerCase()
    }
    if ('Id' in value && value.Id !== undefined) {
      return String(value.Id).toLowerCase()
    }
  }
  return String(value).toLowerCase()
}

const formatReferenceValue = (value, referenceInfo, lookups) => {
  const lookup = lookups[referenceInfo.directoryTypeId]
  if (!lookup) {
    return null
  }

  const resolveLabel = (entry) => {
    const mapped = lookup[normalizeReferenceLookupKey(entry)]
    if (typeof mapped === 'string' && mapped.trim() === '') {
      return null
    }
    return mapped ?? null
  }

  if (Array.isArray(value)) {
    const labels = value
      .map((item) => resolveLabel(item))
      .filter(Boolean)
    return labels.length ? labels.join(', ') : null
  }

  const label = resolveLabel(value)
  return label ?? null
}

const extractFileName = (disposition) => {
  if (!disposition) return null
  const match = /filename="?([^"]+)"?/i.exec(disposition)
  return match ? match[1] : null
}
