export function Pagination({ pagination, onChange }) {
  if (!pagination) return null

  const pages = []
  for (let page = 1; page <= pagination.totalPages; page += 1) {
    if (
      page === 1 ||
      page === pagination.totalPages ||
      Math.abs(page - pagination.pageNumber) <= 1
    ) {
      pages.push(page)
    } else if (pages[pages.length - 1] !== '...') {
      pages.push('...')
    }
  }

  return (
    <div className="pagination">
      <button
        type="button"
        onClick={() => onChange(pagination.pageNumber - 1)}
        disabled={!pagination.hasPrevious}
      >
        Назад
      </button>

      <div className="pagination__pages">
        {pages.map((page, index) =>
          page === '...' ? (
            <span key={`ellipsis-${index}`} className="pagination__ellipsis">
              ...
            </span>
          ) : (
            <button
              key={page}
              type="button"
              className={
                page === pagination.pageNumber
                  ? 'pagination__page pagination__page--active'
                  : 'pagination__page'
              }
              onClick={() => onChange(page)}
            >
              {page}
            </button>
          ),
        )}
      </div>

      <button
        type="button"
        onClick={() => onChange(pagination.pageNumber + 1)}
        disabled={!pagination.hasNext}
      >
        Вперед
      </button>
    </div>
  )
}
