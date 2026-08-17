<script setup lang="ts">
import { ref } from 'vue'

import type {
    TabItem,
    ColumnDef,
    PagedResult,
    WhiptailOption,
    LoginCredentials,
    WhiptailListItem,
} from '../components/types'

// Reactive States
const inputName = ref<string>('John Doe')
const selectedChecklist = ref<Array<string | number>>(['optA'])
const selectedRadio = ref<string | number>('choice1')
const selectedMenu = ref<string | number>('item1')
const logOutput = ref<string[]>([])
const myDate = ref<Date>(new Date())
const formattedDate = ref<string>('')
const selectedTimeSlot = ref<string | number | null>('slot-0900')
const packageName = ref<string>('nginx')
const selectedOption = ref<WhiptailOption | null>(null)
const isInstalled = ref<boolean>(false)
// Form Reactive State
const serverName = ref<string>('srv-web-01')
const environment = ref<string | number | null>('prod')
const notes = ref<string>('Initial node provisioning.')
const errorMessage = ref<string>('')
const envOptions: WhiptailOption[] = [
    { label: 'Production', value: 'prod' },
    { label: 'Staging', value: 'stage' },
    { label: 'Development', value: 'dev' },
]
interface UserDto {
    id: number
    username: string
    email: string
    role: string
    isActive: boolean
}
const columns: ColumnDef<UserDto>[] = [
    { key: 'id', header: 'ID', width: '60px', align: 'center' },
    { key: 'username', header: 'Username' },
    { key: 'email', header: 'Email Address' },
    { key: 'role', header: 'Role', align: 'center' },
    { key: 'isActive', header: 'Status', align: 'center' },
]
const isLoading = ref(false)
// Reactive state matching the C# PagedResult<T> JSON response
const pagedData = ref<PagedResult<UserDto>>({
    items: [
        {
            id: 101,
            username: 'admin',
            email: 'admin@astrolabed.local',
            role: 'SysAdmin',
            isActive: true,
        },
        {
            id: 102,
            username: 'jdoe',
            email: 'jdoe@astrolabed.local',
            role: 'Operator',
            isActive: true,
        },
        {
            id: 103,
            username: 'mbuilder',
            email: 'mbuilder@astrolabed.local',
            role: 'Developer',
            isActive: false,
        },
        {
            id: 104,
            username: 'astrolabe_svc',
            email: 'svc@astrolabed.local',
            role: 'Service',
            isActive: true,
        },
    ],
    totalCount: 42,
    pageNumber: 1,
    pageSize: 10,
    totalPages: 11,
    hasPreviousPage: false,
    hasNextPage: true,
})

const selectedInterface = ref<string>('eth0')

const networkInterfaces: WhiptailListItem[] = [
    {
        tag: 'eth0',
        label: 'Primary Ethernet Interface',
        value: 'eth0',
        description: '192.168.1.100',
    },
    {
        tag: 'wlan0',
        label: 'Wireless Local Area Network',
        value: 'wlan0',
        description: '192.168.1.105',
    },
    { tag: 'lo', label: 'Loopback Adapter', value: 'lo', description: '127.0.0.1' },
]

const handlePageChange = (newPage: number): void => {
    isLoading.value = true
    pagedData.value.pageNumber = newPage
    // Simulate API fetch delay
    setTimeout(() => {
        pagedData.value.hasPreviousPage = newPage > 1
        pagedData.value.hasNextPage = newPage < pagedData.value.totalPages!
        isLoading.value = false
    }, 300)
}

const handleRowClick = (row: UserDto): void => {
    console.log('Selected user:', row)
}

const handleSave = (): void => {
    if (!serverName.value.trim()) {
        errorMessage.value = 'Server Name cannot be empty.'
        return
    }
    errorMessage.value = ''
    console.log('Submitted Configuration:', {
        serverName: serverName.value,
        environment: environment.value,
        notes: notes.value,
    })
}

// Package list options
const packageList: WhiptailOption[] = [
    { label: 'apache2 - HTTP Server', value: 'apache2' },
    { label: 'curl - Command line tool for transferring data', value: 'curl' },
    { label: 'docker-ce - Docker Community Edition', value: 'docker-ce' },
    { label: 'git - Fast, scalable, distributed revision control system', value: 'git' },
    { label: 'nginx - High-performance HTTP server and reverse proxy', value: 'nginx' },
    { label: 'python3 - Interactive high-level language', value: 'python3' },
    { label: 'vim - Vi IMproved text editor', value: 'vim' },
]

const timeSlots: WhiptailOption[] = [
    { label: '09:00 AM - Morning Briefing', value: 'slot-0900' },
    { label: '11:30 AM - System Maintenance', value: 'slot-1130' },
    { label: '02:00 PM - Code Review', value: 'slot-1400' },
    { label: '04:30 PM - Deployment Sync', value: 'slot-1630' },
]
const handleDateConfirm = (selected: Date) => {
    console.log('Selected date:')
}

// Options Data
const checklistOptions: WhiptailOption[] = [
    { label: 'Option A - Core Packages', value: 'optA' },
    { label: 'Option B - Extra Utilities', value: 'optB' },
    { label: 'Option C - Documentation', value: 'optC' },
]

const radioOptions: WhiptailOption[] = [
    { label: 'Choice 1 - Default Engine', value: 'choice1' },
    { label: 'Choice 2 - High Performance', value: 'choice2' },
    { label: 'Choice 3 - Compatibility Mode', value: 'choice3' },
]

const menuOptions: WhiptailOption[] = [
    { label: 'System Diagnostics', value: 'item1' },
    { label: 'Network Configuration', value: 'item2' },
    { label: 'User Management', value: 'item3' },
]

// Helper Logger
const addLog = (msg: string) => {
    logOutput.value.unshift(`[${new Date().toLocaleTimeString()}] ${msg}`)
}

const handleLogin = (credentials: LoginCredentials) => {
    addLog(
        `Login submitted: User="${credentials.username}", Pass="${'*'.repeat(credentials.password.length)}"`,
    )
}

const activeTab = ref('general')

const tabList: TabItem[] = [
    { id: 'general', label: 'General' },
    { id: 'network', label: 'Network' },
    { id: 'system', label: 'System' },
]

const onTabChange = (tabId: string) => {
    console.log(`Active tab changed to: ${tabId}`)
}
</script>

<template>
    <div class="wt-screen">
        <div class="demo-header">
            <h1>Whiptail Vue 3 Component Suite</h1>
            <p>Retro TUI components powered by Vue 3 and TypeScript</p>
        </div>

        <WhiptailTitlebar title="System Configuration Title bar">
            <template #actions>
                <WhiptailButton @click="console.log('help')">?</WhiptailButton>
                <WhiptailButton variant="cancel" @click="console.log('close')">X</WhiptailButton>
            </template>
        </WhiptailTitlebar>

        <!-- Output Event Console -->
        <div class="wt-dialog console-dialog">
            <div class="wt-title">Event Log Output</div>
            <div class="wt-body console-body">
                <div v-if="logOutput.length === 0" class="console-empty">
                    Interact with any component below to see event logs.
                </div>
                <div v-for="(log, idx) in logOutput" :key="idx" class="console-line">
                    {{ log }}
                </div>
            </div>
        </div>

        <!-- Components Showcase -->
        <div class="grid-container">
            <WhiptailMessageBox
                title="1. Message Box"
                message="System alert: Operations are running smoothly."
                @ok="addLog('Message Box: OK clicked')"
            />

            <WhiptailInputBox
                v-model="inputName"
                title="2. Input Box"
                label="Enter target hostname:"
                placeholder="e.g. localhost"
                @submit="(val: any) => addLog(`Input Box Submitted: ${val}`)"
                @cancel="addLog('Input Box: Cancelled')"
            />

            <WhiptailLoginBox
                title="3. Login Authentication"
                @submit="handleLogin"
                @cancel="addLog('Login Box: Cancelled')"
            />

            <WhiptailChecklist
                v-model="selectedChecklist"
                title="4. Package Checklist"
                :options="checklistOptions"
                @ok="(selected: any) => addLog(`Checklist Saved: [${selected.join(', ')}]`)"
            />

            <WhiptailRadiolist
                v-model="selectedRadio"
                title="5. Engine Selection"
                :options="radioOptions"
                @ok="(selected: any) => addLog(`Radio Selected: ${selected}`)"
            />

            <WhiptailMenuList
                v-model="selectedMenu"
                title="6. Main Navigation"
                :items="menuOptions"
                @ok="(selected: any) => addLog(`Menu Selected: ${selected}`)"
            />

            <WhiptailYesNo
                title="7. Confirmation"
                message="Are you sure you want to proceed with format?"
                @yes="addLog('Confirmation: User selected YES')"
                @no="addLog('Confirmation: User selected NO')"
            />

            <WhiptailGauge
                title="8. Task Execution"
                btn-text="Run Process"
                @complete="addLog('Gauge: Progress reached 100%')"
            />

            <WhiptailCalendar
                v-model="myDate"
                title="Select Target Date"
                ok-text="Confirm"
                show-cancel
                @ok="handleDateConfirm"
                @cancel="console.log('Calendar cancelled')"
            />

            <WhiptailSelect
                v-model="selectedTimeSlot"
                :options="timeSlots"
                placeholder="-- Choose Time --"
                @change="console.log('Changed')"
            />

            <WhiptailCombobox
                v-model="packageName"
                :options="packageList"
                placeholder="Type to filter..."
                @select="console.log('Selected')"
            />
        </div>

        <div class="wt-dialog" style="max-width: 500px">
            <div class="wt-title">New Server Provisioning</div>
            <div class="wt-body">
                <!-- 1. Text Input with Validation Error & Required Marker -->
                <WhiptailFormGroup
                    label="Server Name"
                    for-id="srv-name"
                    required
                    hint="Must be a unique hostname identifier."
                    :error="errorMessage"
                >
                    <WhiptailInput
                        id="srv-name"
                        v-model="serverName"
                        prefix="node-"
                        placeholder="e.g. web-01"
                    />
                </WhiptailFormGroup>
                <!-- 2. Select Dropdown in Form Group -->
                <WhiptailFormGroup
                    label="Deployment Environment"
                    hint="Determines cluster group and security level."
                >
                    <WhiptailSelect v-model="environment" :options="envOptions" />
                </WhiptailFormGroup>
                <!-- 3. Textarea in Form Group -->
                <WhiptailFormGroup
                    label="Provisioning Notes"
                    hint="Optional operational context for this node."
                >
                    <WhiptailTextarea
                        v-model="notes"
                        :rows="3"
                        placeholder="Enter additional details..."
                    />
                </WhiptailFormGroup>
            </div>
            <!-- Action Footer -->
            <div class="wt-footer">
                <WhiptailButton variant="ok" @click="handleSave"> Submit </WhiptailButton>
            </div>
        </div>
    </div>

    <div class="wt-dialog" style="max-width: 700px">
        <div class="wt-title">User Accounts Registry</div>
        <div class="wt-body">
            <WhiptailDataGrid
                :data="pagedData"
                :columns="columns"
                :loading="isLoading"
                @page-change="handlePageChange"
                @row-click="handleRowClick"
            >
                <!-- Custom Template for Status Cell -->
                <template #cell-isActive="{ value }">
                    <span :style="{ color: value ? '#00ff00' : '#ff5555' }">
                        {{ value ? '[ ACTIVE ]' : '[ DISABLED ]' }}
                    </span>
                </template>
            </WhiptailDataGrid>
        </div>
    </div>

    <div style="margin: 2rem auto">
        <WhiptailTabs v-model="activeTab" :tabs="tabList" @change="onTabChange">
            <template #general>
                <h3>General Settings</h3>
                <p>Use <strong>Alt + G</strong> to switch to this tab at any time.</p>
                <p>
                    Press <strong>Left/Right Arrows</strong> when focused on a tab to switch tabs.
                </p>
            </template>

            <template #network>
                <h3>Network Configuration</h3>
                <p>Use <strong>Alt + N</strong> to jump directly here.</p>
            </template>

            <template #system>
                <h3>System Status</h3>
                <p>Use <strong>Alt + S</strong> to jump directly here.</p>
            </template>
        </WhiptailTabs>
    </div>

    <div style="max-width: 500px; margin: 20px">
        <h3>Select Primary Network Interface:</h3>
        <WhiptailList v-model="selectedInterface" :items="networkInterfaces" height="180px" />
        <p>
            Selected Value: <code>{{ selectedInterface }}</code>
        </p>
    </div>
</template>

<style scoped>
.demo-header {
    text-align: center;
    margin-bottom: 24px;
    font-family: 'DejaVu Sans Mono', 'Courier New', monospace;
}

.demo-header h1 {
    color: #ffff00;
    font-size: 1.5rem;
    margin-bottom: 6px;
}

.demo-header p {
    color: #a0a0a0;
    font-size: 0.9rem;
}

.console-dialog {
    max-width: 100% !important;
    margin-bottom: 24px !important;
}

.console-body {
    height: 120px;
    overflow-y: auto;
    background-color: #000;
    font-family: inherit;
    font-size: 0.85rem;
}

.console-empty {
    color: #666;
    font-style: italic;
}

.console-line {
    color: #00ff00;
    white-space: pre-wrap;
}

.grid-container {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
    gap: 20px;
}
</style>
