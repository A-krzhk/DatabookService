import { FieldDataType } from '../constants/fieldDataTypes'

const getFieldKey = (field) => field?.columnName ?? field?.ColumnName ?? ''
const getFieldType = (field) => field?.dataType ?? field?.DataType
const getIsCollection = (field) => field?.isCollection ?? field?.IsCollection ?? false
const getIsRequired = (field) => field?.isRequired ?? field?.IsRequired ?? false

const toDateInput = (value) => {
  if (!value) return ''
  if (typeof value === 'string') {
    if (value.length >= 10) return value.slice(0, 10)
    return value
  }
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  return date.toISOString().slice(0, 10)
}

const toDateTimeInput = (value) => {
  if (!value) return ''
  if (typeof value === 'string') {
    if (value.length >= 16) return value.slice(0, 16)
    return value
  }
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const iso = date.toISOString()
  return iso.slice(0, 16)
}

const normalizeBoolean = (raw) => {
  if (typeof raw === 'boolean') return raw
  if (typeof raw === 'string') {
    const normalized = raw.toLowerCase()
    if (normalized === 'true' || normalized === '1') return true
    if (normalized === 'false' || normalized === '0') return false
  }
  if (typeof raw === 'number') {
    return raw === 1
  }
  return false
}

const normalizeCollection = (raw) => {
  if (!raw) return []
  if (Array.isArray(raw)) return raw.map((item) => `${item ?? ''}`).filter((item) => item !== '')
  if (typeof raw === 'string') {
    return raw
      .split('|')
      .map((item) => item.trim())
      .filter(Boolean)
  }
  return []
}

const normalizeScalarValue = (field, raw) => {
  const dataType = getFieldType(field)
  if (raw === null || raw === undefined) {
    if (dataType === FieldDataType.CHECKBOX) return false
    return ''
  }

  if (dataType === FieldDataType.DATE) return toDateInput(raw)
  if (dataType === FieldDataType.DATETIME) return toDateTimeInput(raw)
  if (dataType === FieldDataType.CHECKBOX) return normalizeBoolean(raw)
  if (dataType === FieldDataType.NUMBER) {
    if (typeof raw === 'number') return String(raw)
    const parsed = Number(raw)
    return Number.isNaN(parsed) ? '' : String(parsed)
  }

  return typeof raw === 'string' ? raw : `${raw}`
}

const convertSingleValue = (field, value) => {
  const dataType = getFieldType(field)
  if (value === null || value === undefined) return undefined

  if (dataType === FieldDataType.CHECKBOX) {
    return Boolean(value)
  }

  if (typeof value === 'string') {
    const trimmed = value.trim()
    if (!trimmed) return undefined
    if (dataType === FieldDataType.NUMBER) {
      const numberValue = Number(trimmed)
      return Number.isNaN(numberValue) ? undefined : numberValue
    }
    return trimmed
  }

  if (dataType === FieldDataType.NUMBER) {
    const numberValue = Number(value)
    return Number.isNaN(numberValue) ? undefined : numberValue
  }

  return value
}

export const buildInitialValues = (fields, record = {}) => {
  const values = {}

  fields.forEach((field) => {
    const key = getFieldKey(field)
    const rawValue =
      record[key] ??
      record[field?.columnName] ??
      record[field?.ColumnName]

    if (getIsCollection(field)) {
      values[key] = normalizeCollection(rawValue)
    } else {
      values[key] = normalizeScalarValue(field, rawValue)
    }
  })

  return values
}

export const buildPayload = (fields, values) => {
  const payload = {}

  fields.forEach((field) => {
    const key = getFieldKey(field)
    const currentValue = values[key]

    if (getIsCollection(field)) {
      const list = Array.isArray(currentValue) ? currentValue : []
      const converted = list
        .map((item) => convertSingleValue(field, item))
        .filter(
          (item) => item !== undefined && item !== null && item !== '',
        )

      if (converted.length) {
        payload[key] = converted
      } else if (getIsRequired(field)) {
        payload[key] = []
      }

      return
    }

    if (getFieldType(field) === FieldDataType.CHECKBOX) {
      payload[key] = Boolean(currentValue)
      return
    }

    const converted = convertSingleValue(field, currentValue)

    if (converted !== undefined && converted !== null && converted !== '') {
      payload[key] = converted
    }
  })

  return payload
}

export { getFieldKey }
