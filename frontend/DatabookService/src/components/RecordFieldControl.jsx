import { useEffect, useState } from 'react'
import { apiRequest } from '../api/httpClient'
import { FieldDataType } from '../constants/fieldDataTypes'
import { CollectionInput } from './CollectionInput'

const referenceCache = new Map()

const getKey = (field) => field?.columnName ?? field?.ColumnName
const getType = (field) => field?.dataType ?? field?.DataType
const isCollection = (field) => field?.isCollection ?? field?.IsCollection

export function RecordFieldControl({ apiKey, field, value, onChange }) {
  const type = getType(field)

  if (isCollection(field)) {
    return (
      <CollectionInput
        value={Array.isArray(value) ? value : []}
        onChange={onChange}
        placeholder="Введите значение и нажмите +"
      />
    )
  }

  if (type === FieldDataType.CHECKBOX) {
    return (
      <input
        type="checkbox"
        checked={Boolean(value)}
        onChange={(event) => onChange(event.target.checked)}
      />
    )
  }

  if (type === FieldDataType.REFERENCE) {
    return (
      <ReferenceSelect
        apiKey={apiKey}
        field={field}
        value={value}
        onChange={onChange}
      />
    )
  }

  if (type === FieldDataType.ENUM) {
    const options = field?.enumValues ?? field?.EnumValues ?? []
    if (options.length) {
      return (
        <select value={value ?? ''} onChange={(event) => onChange(event.target.value)}>
          <option value="">Не выбрано</option>
          {options.map((option) => (
            <option key={`${getKey(field)}-${option}`} value={option}>
              {option}
            </option>
          ))}
        </select>
      )
    }
  }

  if (type === FieldDataType.DATE) {
    return (
      <input
        type="date"
        value={value ?? ''}
        onChange={(event) => onChange(event.target.value)}
      />
    )
  }

  if (type === FieldDataType.DATETIME) {
    return (
      <input
        type="datetime-local"
        value={value ?? ''}
        onChange={(event) => onChange(event.target.value)}
      />
    )
  }

  if (type === FieldDataType.NUMBER) {
    return (
      <input
        type="number"
        value={value ?? ''}
        onChange={(event) => onChange(event.target.value)}
      />
    )
  }

  return (
    <input
      type="text"
      value={value ?? ''}
      onChange={(event) => onChange(event.target.value)}
    />
  )
}

function ReferenceSelect({ apiKey, field, value, onChange }) {
  const referenceDirectoryTypeId =
    field.referenceDirectoryTypeId ?? field.ReferenceDirectoryTypeId ?? null
  const { options, loading, error } = useReferenceOptions(
    apiKey,
    referenceDirectoryTypeId,
  )

  if (!referenceDirectoryTypeId) {
    return (
      <input
        type="text"
        value={value ?? ''}
        onChange={(event) => onChange(event.target.value)}
        placeholder="Не выбрано"
      />
    )
  }

  return (
    <div className="reference-select">
      <select
        value={value ?? ''}
        onChange={(event) => onChange(event.target.value || null)}
      >
        <option value="">Не выбрано</option>
        {loading && <option disabled>Загрузка...</option>}
        {options.map((option) => (
          <option key={option.id} value={option.id}>
            {option.label}
          </option>
        ))}
      </select>
      {error && <div className="field-hint field-hint--error">{error}</div>}
    </div>
  )
}

function useReferenceOptions(apiKey, directoryTypeId) {
  const [options, setOptions] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!apiKey?.trim() || !directoryTypeId) {
      setOptions([])
      setError('')
      return
    }

    const cacheKey = `${directoryTypeId}`
    if (referenceCache.has(cacheKey)) {
      setOptions(referenceCache.get(cacheKey))
      return
    }

    let cancelled = false
    setLoading(true)
    setError('')

    apiRequest(`/api/directory-record/${directoryTypeId}/all`, {
      apiKey,
      searchParams: { page: 1, size: 50 },
    })
      .then((response) => {
        if (cancelled) return
        const mapped = mapReferenceOptions(response)
        referenceCache.set(cacheKey, mapped)
        setOptions(mapped)
      })
      .catch((err) => {
        if (cancelled) return
        setError(err.message ?? 'Не удалось загрузить список')
        setOptions([])
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [apiKey, directoryTypeId])

  return { options, loading, error }
}

const mapReferenceOptions = (response) => {
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

  const firstStringName = withoutId.find(
    (column) =>
      isStringType(column.dataType) &&
      column.key.toLowerCase().includes('title'),
  )

  return (
    (nameLike && nameLike.key) ||
    (stringColumn && stringColumn.key) ||
    (firstStringName && firstStringName.key) ||
    (withoutId[0] && withoutId[0].key) ||
    normalized[0].key
  )
}
