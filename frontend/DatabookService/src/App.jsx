import { useEffect, useMemo, useState } from 'react'
import './App.css'
import { Sidebar } from './components/Sidebar'
import { DirectoryTypeBuilder } from './components/DirectoryTypeBuilder'
import { TypeFieldManager } from './components/TypeFieldManager'
import { RecordsPanel } from './components/RecordsPanel'
import { RecordDetail } from './components/RecordDetail'
import { RecordForm } from './components/RecordForm'
import { GroupModal } from './components/GroupModal'
import { useDirectoryTypes } from './hooks/useDirectoryTypes'
import { useDirectoryGroups } from './hooks/useDirectoryGroups'

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
  const {
    groups,
    loading: groupsLoading,
    error: groupsError,
    refresh: refreshGroups,
    createGroup,
    updateGroup,
    deleteGroup,
  } = useDirectoryGroups(apiKey)
  const [groupModalOpen, setGroupModalOpen] = useState(false)
  const [groupModalLoading, setGroupModalLoading] = useState(false)
  const [groupModalError, setGroupModalError] = useState('')

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

  const groupedTypes = useMemo(() => {
    const map = new Map()
    let hasUngrouped = false

    directoryTypes.forEach((type) => {
      const hasGroup = Boolean(type.directoryGroupId)
      const groupId = hasGroup ? type.directoryGroupId : 'ungrouped'
      const groupName = hasGroup
        ? type.directoryGroupName ?? 'Без группы'
        : 'Без группы'
      if (!hasGroup) hasUngrouped = true
      if (!map.has(groupId)) {
        map.set(groupId, { id: groupId, name: groupName, types: [] })
      }
      map.get(groupId).types.push(type)
    })

    groups.forEach((group) => {
      if (!map.has(group.id)) {
        map.set(group.id, { id: group.id, name: group.name, types: [] })
      }
    })

    if (!hasUngrouped && map.has('ungrouped')) {
      map.delete('ungrouped')
    } else if (hasUngrouped && !map.has('ungrouped')) {
      map.set('ungrouped', { id: 'ungrouped', name: 'Без группы', types: [] })
    }

    return Array.from(map.values()).sort((a, b) =>
      a.name.localeCompare(b.name, 'ru'),
    )
  }, [directoryTypes, groups])

  const sidebarGroups = useMemo(
    () => groupedTypes.filter((group) => group.types.length > 0),
    [groupedTypes],
  )

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

  const handleGroupCreate = async (name) => {
    const trimmed = name.trim()
    if (!trimmed) {
      setGroupModalError('Введите название группы')
      return
    }
    try {
      setGroupModalLoading(true)
      setGroupModalError('')
      await createGroup(trimmed)
      await refreshGroups()
      await refetch()
      setGroupModalOpen(false)
    } catch (err) {
      setGroupModalError(err.message)
    } finally {
      setGroupModalLoading(false)
    }
  }

  const handleGroupRename = async (groupId, nextName) => {
    const trimmed = (nextName ?? '').trim()
    if (!trimmed) {
      setGroupModalError('�������� ������ �� ����� ���� ������')
      return
    }
    try {
      setGroupModalLoading(true)
      setGroupModalError('')
      await updateGroup(groupId, trimmed)
      await refreshGroups()
      await refetch()
    } catch (err) {
      setGroupModalError(err.message)
    } finally {
      setGroupModalLoading(false)
    }
  }

  const handleGroupDelete = async (groupId) => {
    if (!groupId) {
      setGroupModalError('�� ������ ������������� ������')
      return
    }
    try {
      setGroupModalLoading(true)
      setGroupModalError('')
      await deleteGroup(groupId)
      await refreshGroups()
      await refetch()
    } catch (err) {
      setGroupModalError(err.message)
    } finally {
      setGroupModalLoading(false)
    }
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
            groups={groups}
            onGroupCreated={refreshGroups}
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
            groups={groups}
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
            groupedTypes={sidebarGroups}
            selectedId={activeTypeId}
            onSelect={handleSelectType}
            onCreateType={handleCreateTypeStart}
            onOpenGroupModal={() => {
              setGroupModalOpen(true)
              setGroupModalError('')
            }}
            onManageType={handleManageType}
            loading={typesLoading}
            disabled={!canLoad}
          />
        )}
        <main className="content-area">{renderContent()}</main>
      </div>

      <GroupModal
        open={groupModalOpen}
        loading={groupModalLoading}
        error={groupModalError}
        groups={groups}
        onClose={() => {
          setGroupModalOpen(false)
          setGroupModalError('')
        }}
        onCreate={handleGroupCreate}
        onRename={handleGroupRename}
        onDelete={handleGroupDelete}
      />
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

