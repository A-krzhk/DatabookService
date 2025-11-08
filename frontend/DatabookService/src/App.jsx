import { useEffect, useMemo, useState } from 'react'
import './App.css'
import { Sidebar } from './components/Sidebar'
import { DirectoryTypeBuilder } from './components/DirectoryTypeBuilder'
import { TypeFieldManager } from './components/TypeFieldManager'
import { RecordsPanel } from './components/RecordsPanel'
import { RecordDetail } from './components/RecordDetail'
import { RecordForm } from './components/RecordForm'
import { useDirectoryTypes } from './hooks/useDirectoryTypes'

const VIEW = {
  PLACEHOLDER: 'placeholder',
  CREATE_TYPE: 'create-type',
  MANAGE_TYPE: 'manage-type',
  RECORDS: 'records',
  RECORD_DETAIL: 'record-detail',
  RECORD_FORM: 'record-form',
}

function App() {
  const [apiKey, setApiKey] = useState(() => {
    if (typeof window === 'undefined') return ''
    return window.localStorage.getItem('databook-api-key') ?? ''
  })
  const [view, setView] = useState({ name: VIEW.PLACEHOLDER })
  const [activeTypeId, setActiveTypeId] = useState(null)

  const {
    items: directoryTypes,
    loading: typesLoading,
    error: typesError,
    canLoad,
    refetch,
  } = useDirectoryTypes(apiKey)

  useEffect(() => {
    if (typeof window === 'undefined') return
    if (apiKey) {
      window.localStorage.setItem('databook-api-key', apiKey)
    } else {
      window.localStorage.removeItem('databook-api-key')
    }
  }, [apiKey])

  const selectedType = useMemo(
    () => directoryTypes.find((type) => type.id === activeTypeId) ?? null,
    [directoryTypes, activeTypeId],
  )

  const showSidebar = Boolean(apiKey)

  const handleSelectType = (typeId) => {
    setActiveTypeId(typeId)
    setView({ name: VIEW.RECORDS })
  }

  const handleCreateTypeStart = () => {
    setActiveTypeId(null)
    setView({ name: VIEW.CREATE_TYPE })
  }

  const handleManageType = (typeId) => {
    setActiveTypeId(typeId)
    setView({ name: VIEW.MANAGE_TYPE })
  }

  const handleRecordOpen = (recordId) => {
    setView({ name: VIEW.RECORD_DETAIL, recordId })
  }

  const handleRecordCreate = () => {
    setView({ name: VIEW.RECORD_FORM })
  }

  const backToRecords = () => {
    setView({ name: VIEW.RECORDS })
  }

  const renderContent = () => {
    if (!apiKey) {
      return (
        <EmptyState
          title="Добавьте API ключ"
          description="Мы используем его во всех запросах. Скопируйте ключ из панели администратора."
        />
      )
    }

    if (typesError) {
      return <ErrorState message={typesError} />
    }

    switch (view.name) {
      case VIEW.CREATE_TYPE:
        return (
          <DirectoryTypeBuilder
            apiKey={apiKey}
            existingTypes={directoryTypes}
            onCreated={(result) => {
              setActiveTypeId(result?.id ?? null)
              setView({ name: VIEW.RECORDS })
            }}
            onCancel={backToRecords}
            onRefreshTypes={refetch}
          />
        )
      case VIEW.MANAGE_TYPE:
        if (!selectedType) {
          return (
            <EmptyState
              title="Справочник не выбран"
              description="Выберите элемент в меню слева."
            />
          )
        }
        return (
          <TypeFieldManager
            apiKey={apiKey}
            type={selectedType}
            directoryTypes={directoryTypes}
            onBack={backToRecords}
            onUpdated={refetch}
          />
        )
      case VIEW.RECORD_DETAIL:
        if (!selectedType) {
          return (
            <EmptyState
              title="Справочник не выбран"
              description="Выберите элемент в меню слева."
            />
          )
        }
        return (
          <RecordDetail
            apiKey={apiKey}
            type={selectedType}
            recordId={view.recordId}
            onBack={backToRecords}
            onDeleted={() => {
              backToRecords()
              refetch()
            }}
          />
        )
      case VIEW.RECORD_FORM:
        if (!selectedType) {
          return (
            <EmptyState
              title="Справочник не выбран"
              description="Выберите элемент в меню слева."
            />
          )
        }
        return (
          <RecordForm
            apiKey={apiKey}
            type={selectedType}
            onCancel={backToRecords}
            onCreated={() => {
              backToRecords()
              refetch()
            }}
          />
        )
      case VIEW.RECORDS:
      default:
        if (!selectedType) {
          return (
            <EmptyState
              title="Выберите справочник"
              description="Выберите элемент слева или создайте новый тип."
            />
          )
        }
        return (
          <RecordsPanel
            apiKey={apiKey}
            type={selectedType}
            onOpenRecord={handleRecordOpen}
            onCreateRecord={handleRecordCreate}
            onRefresh={refetch}
          />
        )
    }
  }

  return (
    <div className="app">
      <header className="app__header">
        <div className="header__brand">
          <h1>Databook Service</h1>
          <p>Управление типами справочников и их записями</p>
        </div>
        <div className="api-key-field">
          <label htmlFor="api-key-input">API ключ</label>
          <input
            id="api-key-input"
            type="password"
            placeholder="dbk_live_xxx"
            value={apiKey}
            onChange={(event) => setApiKey(event.target.value)}
          />
          <button
            type="button"
            className="refresh-button"
            onClick={refetch}
            disabled={!canLoad || typesLoading}
          >
            Обновить
          </button>
        </div>
      </header>

      <div className="app__body">
        {showSidebar && (
          <Sidebar
            types={directoryTypes}
            selectedId={activeTypeId}
            onSelect={handleSelectType}
            onCreateType={handleCreateTypeStart}
            onManageType={handleManageType}
            loading={typesLoading}
            disabled={!canLoad}
          />
        )}
        <main className="content-area">{renderContent()}</main>
      </div>
    </div>
  )
}

function EmptyState({ title, description }) {
  return (
    <div className="empty-state card">
      <h2>{title}</h2>
      <p>{description}</p>
    </div>
  )
}

function ErrorState({ message }) {
  return (
    <div className="panel__error" role="alert">
      {message}
    </div>
  )
}

export default App
