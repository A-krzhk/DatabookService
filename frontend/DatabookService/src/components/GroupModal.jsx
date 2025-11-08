import { useEffect, useState } from 'react'

export function GroupModal({ open, loading, error, onSubmit, onClose }) {
  const [name, setName] = useState('')

  useEffect(() => {
    if (open) {
      setName('')
    }
  }, [open])

  if (!open) return null

  const handleSubmit = () => {
    onSubmit?.(name)
  }

  return (
    <div className="modal-backdrop">
      <div className="modal" role="dialog" aria-modal="true">
        <div className="modal__header">
          <h3>Новая группа</h3>
          <button type="button" className="ghost-button" onClick={onClose}>
            ×
          </button>
        </div>
        <p className="muted">Группы помогают структурировать список справочников.</p>
        {error && <div className="panel__error">{error}</div>}
        <label>
          Название группы
          <input
            type="text"
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="Например, HR, Финансы..."
          />
        </label>
        <div className="modal__actions">
          <button type="button" className="ghost-button" onClick={onClose}>
            Отмена
          </button>
          <button
            type="button"
            className="secondary-button"
            onClick={handleSubmit}
            disabled={!name.trim() || loading}
          >
            {loading ? 'Сохраняем…' : 'Создать группу'}
          </button>
        </div>
      </div>
    </div>
  )
}
