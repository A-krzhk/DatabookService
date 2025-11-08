import { useState } from 'react'

export function CollectionInput({ value = [], onChange, placeholder = 'Введите значение' }) {
  const [draft, setDraft] = useState('')

  const handleAdd = () => {
    const next = draft.trim()
    if (!next) return
    const nextValues = Array.isArray(value) ? [...value, next] : [next]
    onChange(nextValues)
    setDraft('')
  }

  const handleKeyDown = (event) => {
    if (event.key === 'Enter') {
      event.preventDefault()
      handleAdd()
    }
  }

  const handleRemove = (index) => {
    const nextValues = value.filter((_, idx) => idx !== index)
    onChange(nextValues)
  }

  return (
    <div className="collection-input">
      <div className="collection-input__chips">
        {value?.map((item, index) => (
          <span key={`${item}-${index}`} className="chip">
            <span>{item}</span>
            <button
              type="button"
              aria-label="Удалить значение"
              onClick={() => handleRemove(index)}
            >
              ×
            </button>
          </span>
        ))}
      </div>
      <div className="collection-input__controls">
        <input
          type="text"
          value={draft}
          placeholder={placeholder}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={handleKeyDown}
        />
        <button type="button" onClick={handleAdd}>
          +
        </button>
      </div>
    </div>
  )
}
