<script setup lang="ts"></script>

<template>
    <div class="wt-screen">
        <router-view> </router-view>
    </div>
</template>

<style scoped>
.grid-container {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
    gap: 20px;
}
</style>
