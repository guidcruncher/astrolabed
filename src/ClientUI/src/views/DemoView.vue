<script setup lang="ts">
import { ref } from 'vue'

import type { WhiptailOption, LoginCredentials } from '../components/types'

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
    formattedDate.value = selected.toISOString().split('T')[0]
    console.log('Selected date:', selected)
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
</script>

<template>
    <div class="wt-screen">
        <div class="demo-header">
            <h1>Whiptail Vue 3 Component Suite</h1>
            <p>Retro TUI components powered by Vue 3 and TypeScript</p>
        </div>

        <WhiptailTitlebar title="System Configuration Title bar">
            <template #actions>
                <WhiptailButton @click="handleHelp">?</WhiptailButton>
                <WhiptailButton variant="cancel" @click="handleClose">X</WhiptailButton>
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
