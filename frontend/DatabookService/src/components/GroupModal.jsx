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
  onRename,
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

  const handleRename = (groupId) => {
    onRename?.(groupId, drafts[groupId] ?? '')
  }

  const handleDelete = (groupId) => {
    if (!groupId) return
    const confirmed =
      typeof window === 'undefined'
        ? true
        : window.confirm('Delete this group and unassign its directory types?')
    if (confirmed) {
      onDelete?.(groupId)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal" role="dialog" aria-modal="true">
        <div className="modal__header">
          <h3>Manage directory groups</h3>
          <button type="button" className="ghost-button" onClick={onClose}>
            Close
          </button>
        </div>
        <p className="muted">
          Rename existing groups or create a new one for your directory types.
        </p>
        {error && <div className="panel__error">{error}</div>}

        <section className="modal__section">
          <h4>Existing groups</h4>
          {!normalizedGroups.length && (
            <p className="muted">No groups yet.</p>
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
                    onClick={() => handleRename(group.id)}
                    disabled={loading || !(drafts[group.id] ?? '').trim()}
                  >
                    Rename
                  </button>
                  <button
                    type="button"
                    className="ghost-button"
                    onClick={() => handleDelete(group.id)}
                    disabled={loading}
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        </section>

        <section className="modal__section">
          <h4>Create new group</h4>
          <label>
            Group name
            <input
              type="text"
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder="Finance, HR, Operations..."
              disabled={loading}
            />
          </label>
        </section>

        <div className="modal__actions">
          <button type="button" className="ghost-button" onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className="secondary-button"
            onClick={handleCreate}
            disabled={!name.trim() || loading}
          >
            {loading ? 'Saving…' : 'Create group'}
          </button>
        </div>
      </div>
    </div>
  )
}
