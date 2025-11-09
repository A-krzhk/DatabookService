import { useEffect, useMemo, useState } from 'react'

const normalizeGroup = (group) => {
  if (!group) return null
  const id = group.id ?? group.Id ?? null
  if (!id) return null
  return {
    id,
    name: group.name ?? group.Name ?? '',
  }
}

export function GroupModal({
  open,
  loading,
  error,
  groups = [],
  onCreate,
  onUpdate,
  onDelete,
  onClose,
}) {
  const [name, setName] = useState('')
  const [drafts, setDrafts] = useState({})
  const normalizedGroups = useMemo(
    () => groups.map(normalizeGroup).filter(Boolean),
    [groups],
  )

  useEffect(() => {
    if (!open) return
    setName('')
    const nextDrafts = {}
    normalizedGroups.forEach((group) => {
      nextDrafts[group.id] = group.name
    })
    setDrafts(nextDrafts)
  }, [open, normalizedGroups])

  if (!open) return null

  const handleCreate = () => {
    onCreate?.(name)
  }

  const handleUpdate = (groupId) => {
    onUpdate?.(groupId, drafts[groupId] ?? '')
  }

  const handleDelete = (groupId) => {
    if (!groupId) return
    const confirmed =
      typeof window === 'undefined'
        ? true
        : window.confirm('Удалить группу и отвязать от пользователей?')
    if (confirmed) {
      onDelete?.(groupId)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal" role="dialog" aria-modal="true">
        <div className="modal__header">
          <h3>Управление группами пользователей</h3>
          <button type="button" className="ghost-button" onClick={onClose}>
            Закрыть
          </button>
        </div>
        <p className="muted">
          Группировать пользователей можно для удобного поиска или назначения прав.
        </p>
        {error && <div className="panel__error">{error}</div>}

        <section className="modal__section">
          <h4>Существующие группы</h4>
          {!normalizedGroups.length && (
            <p className="muted">Пока нет ни одной группы.</p>
          )}
          <div className="group-modal__list">
            {normalizedGroups.map((group) => (
              <div className="group-modal__row" key={group.id}>
                <input
                  type="text"
                  value={drafts[group.id] ?? ''}
                  onChange={(event) =>
                    setDrafts((prev) => ({
                      ...prev,
                      [group.id]: event.target.value,
                    }))
                  }
                  disabled={loading}
                />
                <div className="group-modal__row-actions">
                  <button
                    type="button"
                    className="secondary-button"
                    onClick={() => handleUpdate(group.id)}
                    disabled={loading || !(drafts[group.id] ?? '').trim()}
                  >
                    Переименовать
                  </button>
                  <button
                    type="button"
                    className="ghost-button"
                    onClick={() => handleDelete(group.id)}
                    disabled={loading}
                  >
                    Удалить
                  </button>
                </div>
              </div>
            ))}
          </div>
        </section>

        <section className="modal__section">
          <h4>Создание группы</h4>
          <label>
            Название группы
            <input
              type="text"
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder="Например, HR, Бухгалтер..."
              disabled={loading}
            />
          </label>
        </section>

        <div className="modal__actions">
          <button type="button" className="ghost-button" onClick={onClose}>
            Отмена
          </button>
          <button
            type="button"
            className="secondary-button"
            onClick={handleCreate}
            disabled={!name.trim() || loading}
          >
            {loading ? 'Сохранение…' : 'Создать группу'}
          </button>
        </div>
      </div>
    </div>
  )
}