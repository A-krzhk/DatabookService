import { apiRequest } from '../api/httpClient'

const referenceCache = new Map()

const buildCacheKey = (apiKey, directoryTypeId) =>
  `${apiKey ?? ''}:${directoryTypeId ?? ''}`

export const getCachedReferenceOptions = (apiKey, directoryTypeId) => {
  const cacheKey = buildCacheKey(apiKey, directoryTypeId)
  return referenceCache.get(cacheKey) ?? null
}

export const clearReferenceOptionsCache = (directoryTypeId, apiKey) => {
  if (directoryTypeId === undefined) {
    referenceCache.clear()
    return
  }
  const cacheKey = buildCacheKey(apiKey, directoryTypeId)
  referenceCache.delete(cacheKey)
}

export const fetchReferenceOptions = async (
  apiKey,
  directoryTypeId,
  searchParams = { page: 1, size: 50 },
  options = {},
) => {
  if (!apiKey?.trim() || !directoryTypeId) return []

  const cacheKey = buildCacheKey(apiKey, directoryTypeId)
  const force = Boolean(options.force)
  if (!force && referenceCache.has(cacheKey)) {
    return referenceCache.get(cacheKey)
  }

  const response = await apiRequest(`/api/directory-record/${directoryTypeId}/all`, {
    apiKey,
    searchParams,
  })

  const optionsList = mapReferenceOptions(response)
  referenceCache.set(cacheKey, optionsList)
  return optionsList
}
export const mapReferenceOptions = (response) => {
  const columns = response?.columns ?? response?.Columns ?? []
  const rows = response?.data ?? response?.Data ?? []
  const labelKey = pickLabelKey(columns)

  return rows.map((row, index) => {
    const id =
      row.Id ??
      row.id ??
      row.ID ??
      row[labelKey] ??
      `row-${index}`
    const label =
      row[labelKey] ??
      row.Name ??
      row.name ??
      row.Title ??
      row.title ??
      String(id)

    return { id, label }
  })
}

const pickLabelKey = (columns) => {
  if (!columns.length) return ''
  const normalized = columns.map((column) => {
    const key = column.fieldName ?? column.FieldName ?? ''
    const dataType = (column.dataType ?? column.DataType ?? '').toLowerCase()
    return { key, dataType }
  })

  const withoutId = normalized.filter(
    (column) => column.key && column.key.toLowerCase() !== 'id',
  )

  const isStringType = (type) =>
    ['string', 'varchar', 'text', 'json', 'char'].includes(type)

  const nameLike = withoutId.find((column) =>
    column.key.toLowerCase().includes('name'),
  )

  const stringColumn = withoutId.find((column) => isStringType(column.dataType))

  const titleColumn = withoutId.find(
    (column) =>
      isStringType(column.dataType) &&
      column.key.toLowerCase().includes('title'),
  )

  return (
    (nameLike && nameLike.key) ||
    (stringColumn && stringColumn.key) ||
    (titleColumn && titleColumn.key) ||
    (withoutId[0] && withoutId[0].key) ||
    normalized[0].key
  )
}

