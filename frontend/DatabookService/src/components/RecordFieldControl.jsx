import { useEffect, useState } from 'react'
import { FieldDataType } from '../constants/fieldDataTypes'
import { CollectionInput } from './CollectionInput'
import {
  fetchReferenceOptions,
  getCachedReferenceOptions,
} from '../utils/referenceOptions'

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

    const cached = getCachedReferenceOptions(apiKey, directoryTypeId)
    if (cached) {
      setOptions(cached)
    }

    let cancelled = false
    setLoading(!cached)
    setError('')

    fetchReferenceOptions(apiKey, directoryTypeId, undefined, { force: true })
      .then((fetched) => {
        if (!cancelled) {
          setOptions(fetched)
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err.message ?? 'Unable to load reference options')
          setOptions([])
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [apiKey, directoryTypeId])

  return { options, loading, error }
}
