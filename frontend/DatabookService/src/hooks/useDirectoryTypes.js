import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { apiRequest } from '../api/httpClient'

const DIRECTORY_TYPES_ENDPOINT = '/api/directory-types'

const normalizeField = (field) => ({
  id: field.id ?? field.Id,
  name: field.name ?? field.Name,
  columnName: field.columnName ?? field.ColumnName,
  dataType: field.dataType ?? field.DataType,
  isRequired: Boolean(field.isRequired ?? field.IsRequired),
  order: field.order ?? field.Order ?? 0,
  isCollection: Boolean(field.isCollection ?? field.IsCollection),
  referenceDirectoryTypeId:
    field.referenceDirectoryTypeId ?? field.ReferenceDirectoryTypeId ?? null,
  referenceDirectoryTypeName:
    field.referenceDirectoryTypeName ?? field.ReferenceDirectoryTypeName ?? '',
  enumValues: field.enumValues ?? field.EnumValues ?? null,
  collectionData:
    field.collectionData?.map((item) => ({
      id: item.id ?? item.Id,
      directoryTypeItemId:
        item.directoryTypeItemId ?? item.DirectoryTypeItemId ?? null,
      value: item.value ?? item.Value,
    })) ?? null,
})

export function useDirectoryTypes(apiKey) {
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const abortRef = useRef(null)

  const canLoad = useMemo(() => Boolean(apiKey?.trim()), [apiKey])

  const fetchDirectoryTypes = useCallback(
    async (signalToUse) => {
      if (!canLoad) {
        setItems([])
        setLoading(false)
        setError('')
        return
      }

      const controller = signalToUse ? null : new AbortController()
      const signal = signalToUse ?? controller.signal
      if (controller) {
        abortRef.current?.abort()
        abortRef.current = controller
      }

      setLoading(true)
      setError('')

      try {
        const payload = await apiRequest(DIRECTORY_TYPES_ENDPOINT, {
          apiKey,
          signal,
        })
        const normalized = (Array.isArray(payload) ? payload : []).map(
          (item) => ({
            id: item.id ?? item.Id,
            name: item.name ?? item.Name ?? 'Untitled',
            description: item.description ?? item.Description ?? '',
            tableName: item.tableName ?? item.TableName ?? '',
            directoryGroupId: item.directoryGroupId ?? item.DirectoryGroupId ?? null,
            directoryGroupName: item.directoryGroupName ?? item.DirectoryGroupName ?? 'Без группы',
            fields: (item.fields ?? item.Fields ?? []).map(normalizeField),
            raw: item,
          }),
        )
        setItems(normalized)
      } catch (err) {
        if (signal.aborted) return
        setError(err.message ?? 'Unexpected error')
        setItems([])
      } finally {
        setLoading(false)
        if (controller) {
          abortRef.current = null
        }
      }
    },
    [apiKey, canLoad],
  )

  useEffect(() => {
    const controller = new AbortController()
    if (canLoad) {
      fetchDirectoryTypes(controller.signal)
    } else {
      abortRef.current?.abort()
    }
    return () => {
      controller.abort()
      abortRef.current?.abort()
    }
  }, [fetchDirectoryTypes, canLoad])

  const refetch = useCallback(() => {
    fetchDirectoryTypes()
  }, [fetchDirectoryTypes])

  return { items, loading, error, canLoad, refetch }
}
