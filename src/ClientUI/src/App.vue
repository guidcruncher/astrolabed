<script setup lang="ts">
import { ref } from 'vue'
import './assets/whiptail.css'

import WhiptailMessageBox from './components/WhiptailMessageBox.vue'
import WhiptailInputBox from './components/WhiptailInputBox.vue'
import WhiptailChecklist from './components/WhiptailChecklist.vue'
import WhiptailRadiolist from './components/WhiptailRadiolist.vue'
import WhiptailMenuList from './components/WhiptailMenuList.vue'
import WhiptailYesNo from './components/WhiptailYesNo.vue'
import WhiptailGauge from './components/WhiptailGauge.vue'
import WhiptailLoginBox from './components/WhiptailLoginBox.vue'

import type { WhiptailOption, LoginCredentials } from './components/types'

// Reactive States
const inputName = ref<string>('John Doe')
const selectedChecklist = ref<Array<string | number>>(['optA'])
const selectedRadio = ref<string | number>('choice1')
const selectedMenu = ref<string | number>('item1')
const logOutput = ref<string[]>([])

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
                @submit="(val) => addLog(`Input Box Submitted: ${val}`)"
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
                @ok="(selected) => addLog(`Checklist Saved: [${selected.join(', ')}]`)"
            />

            <WhiptailRadiolist
                v-model="selectedRadio"
                title="5. Engine Selection"
                :options="radioOptions"
                @ok="(selected) => addLog(`Radio Selected: ${selected}`)"
            />

            <WhiptailMenuList
                v-model="selectedMenu"
                title="6. Main Navigation"
                :items="menuOptions"
                @ok="(selected) => addLog(`Menu Selected: ${selected}`)"
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
