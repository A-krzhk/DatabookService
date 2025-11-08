export function Sidebar({
  types,
  selectedId,
  onSelect,
  onCreateType,
  onManageType,
  loading,
  disabled,
}) {
  return (
    <aside className="sidebar">
      <div className="sidebar__header">
        <div>
          <span className="sidebar__title">Справочники</span>
          {loading && <span className="sidebar__status">Обновляем...</span>}
        </div>
        <button
          type="button"
          className="sidebar__action-button"
          onClick={onCreateType}
          disabled={disabled}
        >
          +
        </button>
      </div>

      {!types.length && (
        <p className="sidebar__hint">
          {disabled
            ? 'Добавьте API ключ, чтобы увидеть список справочников.'
            : 'Пока нет ни одного справочника. Создайте новый.'}
        </p>
      )}

      <ul className="sidebar__list">
        {types.map((type) => (
          <li key={type.id}>
            <button
              type="button"
              className={
                type.id === selectedId
                  ? 'sidebar__item sidebar__item--active'
                  : 'sidebar__item'
              }
              onClick={() => onSelect(type.id)}
              disabled={disabled}
            >
              <span className="sidebar__item-name">{type.name}</span>
              <span className="sidebar__item-meta">{type.tableName}</span>
            </button>
            <button
              type="button"
              className="sidebar__gear"
              onClick={(event) => {
                event.stopPropagation()
                onManageType(type.id)
              }}
              disabled={disabled}
              aria-label={`Настроить ${type.name}`}
            >
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  d="M12 9.5a2.5 2.5 0 1 0 0 5 2.5 2.5 0 0 0 0-5Zm8.94 2.24-1.38-.8a7.03 7.03 0 0 0-.32-.73l.2-1.58a.75.75 0 0 0-.62-.83l-1.8-.32a6.88 6.88 0 0 0-.72-.42l-.9-1.53a.75.75 0 0 0-.95-.3l-1.66.7c-.25-.1-.51-.18-.78-.24l-.76-1.71a.75.75 0 0 0-.84-.43l-1.8.32a.75.75 0 0 0-.62.83l.2 1.58c-.11.24-.22.48-.32.73l-1.38.8a.75.75 0 0 0-.27 1.02l.9 1.53c-.03.27-.03.54 0 .81l-.9 1.53a.75.75 0 0 0 .27 1.02l1.38.8c.1.25.21.49.32.73l-.2 1.58a.75.75 0 0 0 .62.83l1.8.32c.16.26.33.51.51.75l-.52 1.67a.75.75 0 0 0 .5.94l1.73.55c.32.1.67-.05.81-.37l.66-1.52c.28-.05.56-.12.83-.2l1.23 1.08c.25.22.62.22.87 0l1.27-1.11c.26.08.53.14.8.19l.66 1.52a.75.75 0 0 0 .81.37l1.73-.55a.75.75 0 0 0 .5-.94l-.52-1.67c.18-.24.35-.49.51-.75l1.8-.32a.75.75 0 0 0 .62-.83l-.2-1.58c.11-.24.22-.48.32-.73l1.38-.8a.75.75 0 0 0 .27-1.02l-.9-1.53c.03-.27.03-.54 0-.81l.9-1.53a.75.75 0 0 0-.27-1.02ZM12 15a3 3 0 1 1 0-6 3 3 0 0 1 0 6Z"
                  fill="currentColor"
                />
              </svg>
            </button>
          </li>
        ))}
      </ul>
    </aside>
  )
}
