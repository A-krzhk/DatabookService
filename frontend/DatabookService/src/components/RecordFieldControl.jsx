import { FieldDataType } from '../constants/fieldDataTypes'
import { CollectionInput } from './CollectionInput'

const getKey = (field) => field?.columnName ?? field?.ColumnName
const getType = (field) => field?.dataType ?? field?.DataType
const isCollection = (field) => field?.isCollection ?? field?.IsCollection

export function RecordFieldControl({ field, value, onChange }) {
  const type = getType(field)

  if (isCollection(field)) {
    return (
      <CollectionInput
        value={Array.isArray(value) ? value : []}
        onChange={onChange}
        placeholder="Добавьте значение и нажмите +"
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
