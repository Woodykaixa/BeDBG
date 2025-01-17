<script setup lang="ts">
import { onMounted, onUnmounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useNotification, NLayout, NLayoutSider, NLayoutContent } from 'naive-ui';
import { Api } from '@/api';
import { DebuggerEventSource } from '@/util/debuggerEventSource';
import DebugViewSider from '@/components/DebugViewSider.vue';
import { provideDebuggerEventSource } from '@/hooks/useDebuggerEvent';
import { useDebugData, WinProcess, WinThread } from '@/hooks/useDebugData';
import DebugView from '@/components/DebugView.vue';
import { useLoadingStates } from '@/hooks/useLoadingStates';
import { usePlugin } from '@/hooks/usePlugin';
import { DefaultEmptyRegisters } from '@/dto/thread';

const debuggerEvent = new DebuggerEventSource('/api/debugger/0/event');
const pluginStore = usePlugin();

onUnmounted(() => {
  pluginStore.removeEventSource();
});

debuggerEvent.addEventListener('notFound', () => {
  debuggerEvent.close(); // If debugger not present, close the event source
  pluginStore.removeEventSource();
});

const debugData = useDebugData();
const loadingStates = useLoadingStates();

const programReady = ref(false);

debuggerEvent.addEventListenerOnce('programReady', () => {
  programReady.value = true;
});

provideDebuggerEventSource(debuggerEvent);

const router = useRouter();
const notification = useNotification();

async function InitializeDebugger() {
  pluginStore.setEventSource(debuggerEvent);
  debuggerEvent.addEventListener('createProcess', e => {
    const mainThread: WinThread = reactive({
      address: e.threadLocalBase,
      id: e.thread,
      entry: e.startAddress,
      instructions: [],
      registers: DefaultEmptyRegisters,
    });

    const mainProcess: WinProcess = reactive({
      id: e.process,
      mainThread,
      threads: new Map([[mainThread.id, mainThread]]),
    });

    debugData.$patch({
      mainProcess: mainProcess,
      process: new Map([[e.process, mainProcess]]),
    });

    // Api.DebuggingProcess.disassemble(e.process, e. )
  });

  debuggerEvent.addEventListener('createThread', e => {
    const thread: WinThread = {
      address: e.threadLocalBase,
      id: e.thread,
      entry: e.startAddress,
      instructions: [],
      registers: DefaultEmptyRegisters,
    };
    // createThread event means it created a new thread on a old process. If both process and thread are created, it will be a createProcess event.
    // Thus we can use '!.' to skip falsy check
    debugData.process.get(e.process)!.threads.set(e.thread, thread);
  });

  debuggerEvent.addEventListener('exitProgram', stopDebug);
  debuggerEvent.addEventListenerOnce('breakpoint', async exception => {
    loadingStates.disassemblyState = 'loading';
    loadingStates.panelState = 'ready';
    const { ok, data } = await Api.DebuggingProcess.disassemble(
      debugData.mainProcess.id,
      exception.exceptionAddress + 1
    );
    if (ok) {
      loadingStates.disassemblyState = 'ready';
      debugData.mainProcess.mainThread.instructions = data.map(i => ({
        address: i.ip,
        text: i.text,
      }));
    }
  });
  debuggerEvent.addEventListenerOnce('breakpoint', exception => {
    debugData.updateRegisters(exception.process, exception.thread);
  });
  debuggerEvent.addEventListener('programReady', async () => {
    loadingStates.debuggerReady = true;
    loadingStates.panelState = 'ready';
    // updateRegisters();
    debugData.syncBreakpoints();

    debuggerEvent.addEventListener('exception', exception => {
      console.error('exception', exception);
    });
  });
}

async function CheckDebugging() {
  const { data, ok } = await Api.DebuggingProcess.list();
  if (ok) {
    if (data.length === 0) {
      notification.error({
        title: '未指定调试进程',
        content: '请附加或创建一个进程来调试',
      });
      router.push('/');
      return;
    }
  } else {
    notification.error({
      title: '无法获取调试中进程',
      description: data.error,
      content: data.message,
    });
    router.push('/');
    return;
  }
}

onMounted(() => {
  CheckDebugging(); // If debugger is not present, we will redirect to home page.
  InitializeDebugger(); // So we don't need to check return value here.
});

const stopDebug = async () => {
  sessionStorage.removeItem('debugPid');
  const { ok, data } = await Api.DebuggingProcess.detachProcess(debugData.mainProcess.id);
  if (ok) {
    router.push('/');
  } else {
    notification.error({
      title: '退出调试失败',
      description: data.error,
      content: data.message,
    });
  }
};
</script>

<template>
  <n-layout has-sider>
    <!-- <n-layout-sider
      collapse-mode="transform"
      :collapsed-width="0"
      :width="800"
      show-trigger="arrow-circle"
      content-style="padding: 24px; height:100vh;"
      bordered
      :native-scrollbar="false"
    > -->
      <!-- <debug-view-sider v-if="programReady" /> -->
    <!-- </n-layout-sider> -->
    <n-layout-content>
      <debug-view v-if="programReady" />
    </n-layout-content>
  </n-layout>
</template>
