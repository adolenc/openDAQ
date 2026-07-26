#include <copendaq.h>

#include <gtest/gtest.h>

using COpendaqSchedulerTest = testing::Test;

static daqCBool taskCalled = daqFalse;
static daqCBool taskGraphCalled = daqFalse;
static daqCBool functionCalled = daqFalse;

daqErrCode procedureTask(daqBaseObject*)
{
    printf("task called\n");
    taskCalled = true;
    return 0;
}

daqErrCode procedureTaskGraph(daqBaseObject*)
{
    printf("taskGraph called\n");
    taskGraphCalled = true;
    return 0;
}

daqErrCode functionCall(daqBaseObject*, daqBaseObject**)
{
    printf("function called\n");
    functionCalled = true;
    return 0;
}

TEST_F(COpendaqSchedulerTest, Scheduler)
{
    daqList* sinks = nullptr;
    daqList_createList(&sinks);

    daqLoggerSink* sink = nullptr;
    daqLoggerSink_createStdErrLoggerSink(&sink);
    daqList_pushBack(sinks, sink);

    daqLogger* logger = nullptr;
    daqLogger_createLogger(&logger, sinks, daqLogLevel::daqLogLevelDebug);

    daqScheduler* scheduler = nullptr;
    daqScheduler_createScheduler(&scheduler, logger, 1);
    ASSERT_NE(scheduler, nullptr);

    daqProcedure* procGraph = nullptr;
    daqProcedure_createProcedure(&procGraph, procedureTaskGraph);

    daqString* nameGraph = nullptr;
    daqString_createString(&nameGraph, "taskGraph");

    daqTaskGraph* taskGraph = nullptr;
    daqTaskGraph_createTaskGraph(&taskGraph, procGraph, nameGraph);

    daqTask* task = nullptr;
    daqBaseObject_borrowInterface(taskGraph, DAQ_TASK_INTF_ID, (void**) &task);
    ASSERT_NE(task, nullptr);

    daqProcedure* taskProc = nullptr;
    daqProcedure_createProcedure(&taskProc, procedureTask);

    daqString* name = nullptr;
    daqString_createString(&name, "task");

    daqTask* task2 = nullptr;
    daqTask_createTask(&task2, taskProc, name);

    daqTask_then(task, task2);

    daqAwaitable* awaitable = nullptr;
    daqScheduler_scheduleGraph(scheduler, taskGraph, &awaitable);
    ASSERT_NE(awaitable, nullptr);

    daqFunction* function = nullptr;
    daqFunction_createFunction(&function, functionCall);

    daqAwaitable* awaitable2 = nullptr;
    daqScheduler_scheduleFunction(scheduler, function, &awaitable2);
    ASSERT_NE(awaitable2, nullptr);

    daqScheduler_waitAll(scheduler);

    ASSERT_EQ(taskCalled, daqTrue);
    ASSERT_EQ(functionCalled, daqTrue);
    ASSERT_EQ(taskGraphCalled, daqTrue);

    daqUnknown_releaseRef(awaitable2);
    daqUnknown_releaseRef(function);

    daqUnknown_releaseRef(awaitable);
    daqUnknown_releaseRef(task2);
    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(taskProc);

    daqUnknown_releaseRef(taskGraph);
    daqUnknown_releaseRef(procGraph);
    daqUnknown_releaseRef(nameGraph);

    daqUnknown_releaseRef(scheduler);
    daqUnknown_releaseRef(logger);
    daqUnknown_releaseRef(sink);
    daqUnknown_releaseRef(sinks);
}