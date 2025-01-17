<script setup lang="ts">
import { ref, effect, onMounted, watch, h, reactive } from 'vue';
import {
  NList,
  NListItem,
  NThing,
  NScrollbar,
  NEmpty,
  NSpin,
  NIcon,
  NButton,
  NBreadcrumb,
  NBreadcrumbItem,
  NInput,
  InputInst,
  NResult,
  useNotification,
} from 'naive-ui';
import { Api } from '@/api';
import { FileModel } from '@/dto/fs';
import { FileOutlined, FolderOutlined } from '@vicons/antd';
import { useRouter } from 'vue-router';

const notification = useNotification();

const cwd = ref('');
const folders = ref([] as string[]);
const errorData = reactive({
  hasError: false,
  msg: null as null | string,
  err: null as null | string,
});
watch(
  () => cwd.value,
  () => {
    folders.value = cwd.value.split('\\');
  }
);

const loading = ref(true);
const files = ref([] as Array<FileModel>);
const targetFile = ref('');
const requestFileList = (dir?: string) => {
  effect(async () => {
    loading.value = true;
    targetFile.value = '';
    const { data, ok } = await Api.getFileList(dir);
    errorData.hasError = !ok;
    loading.value = false;
    if (ok) {
      files.value = data.files;
      cwd.value = data.path;
    } else {
      cwd.value = dir ?? '';
      errorData.msg = data.message;
      errorData.err = data.error;
    }
  });
};

onMounted(requestFileList);

const clickBreadcrumb = (index: number) => {
  const path = folders.value.slice(0, index + 1).join('\\');
  requestFileList(path);
};

const router = useRouter();
const command = ref<InputInst>(null as unknown as InputInst);
const debugExe = async () => {
  const cmd = command.value.inputElRef!.value;
  const file = targetFile.value;
  loading.value = true;
  const { ok, data } = await Api.DebuggingProcess.create(file, cmd);
  loading.value = false;
  if (ok) {
    router.push('/debug');
  } else {
    notification.error({
      title: '创建进程失败',
      description: data.error,
      content: data.message,
    });
  }
};
</script>

<template>
  <n-spin :show="loading">
    <n-list class="panel">
      <template #header>
        <n-breadcrumb separator="/">
          <n-breadcrumb-item v-for="(path, index) in folders" :key="index">
            <n-button text @click="clickBreadcrumb(index)">{{ path }}</n-button>
          </n-breadcrumb-item>
        </n-breadcrumb>
      </template>
      <div style="display: flex; width: 100%; margin-bottom: 8px">
        <n-input placeholder="启动命令" ref="command" style="flex: 1%; margin-right: 4px" />
        <n-button :disabled="targetFile === ''" @click="debugExe">启动</n-button>
      </div>
      <n-result
        v-if="errorData.hasError"
        class="panel-error"
        status="error"
        :description="errorData.msg!"
        :title="errorData.err!"
      />
      <n-scrollbar v-else-if="files.length !== 0" style="height: 50vh; padding: 8px; background-color: rgb(26, 26, 26)">
        <n-list-item
          v-for="file in files"
          :key="file.name"
          class="file-item"
          :class="targetFile === file.path ? ['file-item-selected'] : []"
        >
          <n-thing @click="file.type === 'folder' ? requestFileList(file.path) : (targetFile = file.path)">
            <template #header>
              <div>{{ file.name }}</div>
            </template>
            <template #description>
              {{ file.path }}
            </template>
            <template #avatar>
              <n-icon size="24">
                <file-outlined v-if="file.type === 'file'" />
                <folder-outlined v-else-if="file.type === 'folder'" />
              </n-icon>
            </template>
          </n-thing>
        </n-list-item>
      </n-scrollbar>
      <n-empty v-else description="无可调试文件" class="panel-empty"> </n-empty>
    </n-list>
  </n-spin>
</template>

<style scoped>
.panel {
  width: 100%;
  height: 60vh;
  position: relative;
}

.panel-error {
  height: 50vh;
  padding: 8px;
  background-color: rgb(26, 26, 26);
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.panel-empty {
  width: 100%;
  height: 100%;
  position: absolute;
  left: 50%;
  top: 50%;
  transform: translate(-50%, -50%);
  justify-content: center;
  pointer-events: none;
}
.file-item {
  cursor: pointer;
  padding: 4px;
}

.file-item-selected {
  background-color: var(--hover-color);
}

.file-item-selected * {
  color: var(--primary-color-hover) !important;
}

.file-item:hover {
  background-color: var(--hover-color);
  transition-timing-function: var(--n-bezier);
  transition: 0.3s;
}
</style>
