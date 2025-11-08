export const FieldDataType = Object.freeze({
  STRING: 1,
  NUMBER: 2,
  IDENTIFIER: 3,
  CHECKBOX: 4,
  REFERENCE: 5,
  DATE: 6,
  DATETIME: 7,
  ENUM: 8,
})

export const FIELD_DATA_TYPES = [
  { value: FieldDataType.STRING, label: 'Строка' },
  { value: FieldDataType.NUMBER, label: 'Число' },
  { value: FieldDataType.IDENTIFIER, label: 'Идентификатор' },
  { value: FieldDataType.CHECKBOX, label: 'Да/Нет' },
  { value: FieldDataType.REFERENCE, label: 'Справочник' },
  { value: FieldDataType.DATE, label: 'Дата' },
  { value: FieldDataType.DATETIME, label: 'Дата и время' },
  { value: FieldDataType.ENUM, label: 'Справочник значений' },
]

const typeMap = FIELD_DATA_TYPES.reduce((acc, item) => {
  acc[item.value] = item.label
  return acc
}, {})

export const getFieldTypeLabel = (value) => typeMap[value] ?? `Тип ${value}`

export const requiresReferenceDirectory = (value) =>
  Number(value) === FieldDataType.REFERENCE

export const requiresEnumValues = (value) => Number(value) === FieldDataType.ENUM
