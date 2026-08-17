<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAstrolabedApi, type DiscoveredLanDevice } from '../composables/useAstrolabedApi'
import type { ColumnDef, PagedResult } from './types'

const { loading, getDiscoveredNetworkDevices } = useAstrolabedApi()

const lanDevicesPaged = ref<PagedResult<DiscoveredLanDevice>>({
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
})

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

const fetchDevices = async (): Promise<void> => {
    try {
        const result = (await getDiscoveredNetworkDevices({
            pageNumber: lanPageNumber.value,
            pageSize: lanPageSize.value,
        })) as PagedResult<DiscoveredLanDevice>

        lanDevicesPaged.value = result || {
            items: [],
            pageNumber: lanPageNumber.value,
            pageSize: lanPageSize.value,
            totalCount: 0,
        }
    } catch {
        lanDevicesPaged.value = {
            items: [],
            pageNumber: lanPageNumber.value,
            pageSize: lanPageSize.value,
            totalCount: 0,
        }
    }
}

const handlePageChange = (page: number): void => {
    lanPageNumber.value = page
    fetchDevices()
}

const handlePageSizeChange = (size: number): void => {
    lanPageSize.value = size
    lanPageNumber.value = 1
    fetchDevices()
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
