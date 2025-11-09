import { useCallback, useEffect, useMemo, useState } from 'react'
import { apiRequest } from '../api/httpClient'

const GROUPS_ENDPOINT = '/api/directory-groups'

export function useDirectoryGroups(apiKey) {
  const [groups, setGroups] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const fetchGroups = useCallback(async () => {
    if (!apiKey?.trim()) {
      setGroups([])
      setError('')
      return
    }
    setLoading(true)
    setError('')
    try {
      const data = await apiRequest(GROUPS_ENDPOINT, {
        apiKey,
      })
      setGroups(Array.isArray(data) ? data : [])
    } catch (requestError) {
      setError(requestError.message)
      setGroups([])
    } finally {
      setLoading(false)
    }
  }, [apiKey])

  useEffect(() => {
    fetchGroups()
  }, [fetchGroups])

  const refresh = useCallback(() => {
    fetchGroups()
  }, [fetchGroups])

  const createGroup = useCallback(
    async (name) => {
      if (!apiKey?.trim()) {
        throw new Error('API key is missing')
      }
      const created = await apiRequest(GROUPS_ENDPOINT, {
        method: 'POST',
        apiKey,
        body: { name },
      })
      refresh()
      return created
    },
    [apiKey, refresh],
  )

  const updateGroup = useCallback(
    async (id, name) => {
      if (!apiKey?.trim()) {
        throw new Error('API key is missing')
      }
      if (!id) {
        throw new Error('Group id is missing')
      }
      await apiRequest(`${GROUPS_ENDPOINT}/${id}`, {
        method: 'PUT',
        apiKey,
        body: { name },
      })
      refresh()
    },
    [apiKey, refresh],
  )

  const deleteGroup = useCallback(
    async (id) => {
      if (!apiKey?.trim()) {
        throw new Error('API key is missing')
      }
      if (!id) {
        throw new Error('Group id is missing')
      }
      await apiRequest(`${GROUPS_ENDPOINT}/${id}`, {
        method: 'DELETE',
        apiKey,
      })
      refresh()
    },
    [apiKey, refresh],
  )

  return { groups, loading, error, refresh, createGroup, updateGroup, deleteGroup }
}
