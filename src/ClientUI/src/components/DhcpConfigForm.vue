<script setup lang="ts">
import { type DhcpOptions } from '../composables/useServerOptions'
const config = defineModel<DhcpOptions>({ required: true })
</script>

<template>
    <div class="wt-section-body">
        <div class="wt-form-row wt-form-group">
            <div style="flex: 1">
                <WhiptailCheckbox v-model="config.Enabled" label="Enable DHCP Server" />
            </div>
            <div style="flex: 2">
                <label class="wt-label">Listen Address</label>
                <input v-model="config.ListenAddress" type="text" class="wt-input" />
            </div>
            <div style="flex: 1">
                <label class="wt-label">Listen Port</label>
                <input v-model.number="config.ListenPort" type="number" class="wt-input" />
            </div>
        </div>

        <!-- Addressing & Network Settings -->
        <div class="wt-message wt-form-group">
            <span class="wt-label" style="color: #ffff00; font-weight: bold"
                >Subnet & Addressing Configuration</span
            >
            <div class="wt-form-row" style="margin-top: 8px">
                <div style="flex: 1">
                    <label class="wt-label">Pool CIDR</label>
                    <input v-model="config.PoolCidr" type="text" class="wt-input" />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">Server Identifier</label>
                    <input v-model="config.ServerIdentifier" type="text" class="wt-input" />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">Router / Gateway</label>
                    <input v-model="config.Router" type="text" class="wt-input" />
                </div>
            </div>
            <div class="wt-form-row" style="margin-top: 8px">
                <div style="flex: 1">
                    <label class="wt-label">DNS Server</label>
                    <input v-model="config.DnsServer" type="text" class="wt-input" />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">NTP Server</label>
                    <input v-model="config.NtpServer" type="text" class="wt-input" />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">Domain Name</label>
                    <input v-model="config.DomainName" type="text" class="wt-input" />
                </div>
            </div>
        </div>

        <!-- Leases & PXE Booting -->
        <div class="wt-message wt-form-group">
            <span class="wt-label" style="color: #ffff00; font-weight: bold"
                >Leases & Boot Parameters</span
            >
            <div class="wt-form-row" style="margin-top: 8px">
                <div style="flex: 1">
                    <label class="wt-label">Lease Duration (Hours)</label>
                    <input v-model.number="config.LeaseHours" type="number" class="wt-input" />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">ARP Timeout (ms)</label>
                    <input v-model.number="config.ArpTimeoutMs" type="number" class="wt-input" />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">Interface MTU</label>
                    <input v-model.number="config.InterfaceMtu" type="number" class="wt-input" />
                </div>
            </div>
            <div class="wt-form-row" style="margin-top: 8px">
                <div style="flex: 1">
                    <label class="wt-label">TFTP Server Name</label>
                    <input v-model="config.TftpServerName" type="text" class="wt-input" />
                </div>
                <div style="flex: 1">
                    <label class="wt-label">Bootfile Name</label>
                    <input v-model="config.BootfileName" type="text" class="wt-input" />
                </div>
            </div>
            <div class="wt-form-group" style="margin-top: 8px">
                <label class="wt-label">Web Proxy Auto-Discovery (WPAD) URL</label>
                <input v-model="config.WebProxyServerUrl" type="text" class="wt-input" />
            </div>
        </div>

        <!-- Path Storage Storage Locations -->
        <div class="wt-form-group">
            <label class="wt-label">Lease Store Path</label>
            <input v-model="config.LeaseStorePath" type="text" class="wt-input" />
        </div>
        <div class="wt-form-group">
            <label class="wt-label">Bad IP Store Path</label>
            <input v-model="config.BadIpStorePath" type="text" class="wt-input" />
        </div>
    </div>
</template>

<style scoped>
.wt-section-body {
    display: flex;
    flex-direction: column;
    gap: 12px;
}
</style>
