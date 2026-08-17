<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAstrolabedApi, type DiscoveredLanDevice } from '../composables/useAstrolabedApi'
import type { ColumnDef, PagedResult } from './types'

const { loading, getDiscoveredNetworkDevices } = useAstrolabedApi()

const devices = ref<DiscoveredLanDevice[]>([])
const lanPageNumber = ref<number>(1)
const lanPageSize = ref<number>(10)

const lanDeviceColumns: ColumnDef<DiscoveredLanDevice>[] = [
    {
        key: 'hostName',
        header: 'Host Name',
        formatter: (row: DiscoveredLanDevice) => row.hostName || '< Unknown >',
    },
    { key: 'ipAddress', header: 'IP Address' },
    { key: 'macAddress', header: 'MAC Address' },
]

const lanDevicesPaged = computed<PagedResult<DiscoveredLanDevice>>(() => {
    const totalCount = devices.value.length
    const startIndex = (lanPageNumber.value - 1) * lanPageSize.value
    const endIndex = startIndex + lanPageSize.value
    const items = devices.value.slice(startIndex, endIndex)

    return {
        items,
        pageNumber: lanPageNumber.value,
        pageSize: lanPageSize.value,
        totalCount,
    }
})

const fetchDevices = async (): Promise<void> => {
    try {
        const result = await getDiscoveredNetworkDevices()
        devices.value = Array.isArray(result) ? result : []
        lanPageNumber.value = 1
    } catch {
        devices.value = []
    }
}

const handlePageChange = (page: number): void => {
    lanPageNumber.value = page
}

const handlePageSizeChange = (size: number): void => {
    lanPageSize.value = size
    lanPageNumber.value = 1
}

onMounted(() => {
    fetchDevices()
})
</script>

<template>
    <WhiptailDataGrid
        :columns="lanDeviceColumns"
        :data="lanDevicesPaged"
        :loading="loading"
        @page-change="handlePageChange"
        @page-size-change="handlePageSizeChange"
    />
</template>
