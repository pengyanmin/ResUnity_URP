using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 使用UniTask所需的命名空间
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System;

public class UniTaskTral : MonoBehaviour
{
    private bool isActive = false;
    // Start is called before the first frame update
    void Start()
    {
        // 加载不销毁嘿嘿
        DontDestroyOnLoad(this);
        
        DemoAsync().Forget();
    }

    // 你可以返回一个形如 UniTask<T>(或 UniTask) 的类型，这种类型事为Unity定制的，作为替代原生Task<T>的轻量级方案
    // 为Unity集成的 0GC，快速调用，0消耗的 async/await 方案
    async UniTask<string> DemoAsync()
    {
        // 你可以等待一个Unity异步对象
        var asset = await Resources.LoadAsync<TextAsset>("foo");
        var txt = (await UnityWebRequest.Get("http://127.0.0.1:5000").SendWebRequest()).downloadHandler.text;
        await SceneManager.LoadSceneAsync("scene2");

        // .WithCancellation 会启用取消功能，GetCancellationTokenOnDestroy 表示获取一个依赖对象生命周期的Cancel句柄，当对象被销毁时，将会调用这个Cancel句柄，从而实现取消的功能
        var asset2 = await Resources.LoadAsync<TextAsset>("bar").WithCancellation(this.GetCancellationTokenOnDestroy());

        // .ToUniTask 可接收一个 progress 回调以及一些配置参数，Progress.Create是IProgress<T>的轻量级替代方案
        var asset3 = await Resources.LoadAsync<TextAsset>("baz").ToUniTask(Progress.Create<float>(x => Debug.Log(x)));

        // 等待一个基于帧的延时操作（就像一个协程一样）
        await UniTask.DelayFrame(100);

        // yield return new WaitForSeconds/WaitForSecondsRealtime 的替代方案
        await UniTask.Delay(TimeSpan.FromSeconds(10), ignoreTimeScale: false);

        // 可以等待任何 playerloop 的生命周期(PreUpdate, Update, LateUpdate, 等...)
        await UniTask.Yield(PlayerLoopTiming.PreLateUpdate);

        // yield return null 替代方案
        await UniTask.Yield();
        await UniTask.NextFrame();

        // WaitForEndOfFrame 替代方案 (需要 MonoBehaviour(CoroutineRunner))
        await UniTask.WaitForEndOfFrame(this); // this 是一个 MonoBehaviour

        // yield return new WaitForFixedUpdate 替代方案，(和 UniTask.Yield(PlayerLoopTiming.FixedUpdate) 效果一样)
        await UniTask.WaitForFixedUpdate();

        // yield return WaitUntil 替代方案
        await UniTask.WaitUntil(() => isActive == false);

        // WaitUntil拓展，指定某个值改变时触发
        await UniTask.WaitUntilValueChanged(this, x => x.isActive);

        // 你可以直接 await 一个 IEnumerator 协程
        await FooCoroutineEnumerator();

        // 你可以直接 await 一个原生 task
        await Task.Run(() => 100);

        // 多线程示例，在此行代码后的内容都运行在一个线程池上
        await UniTask.SwitchToThreadPool();

        /* 工作在线程池上的代码 */

        // 转回主线程
        await UniTask.SwitchToMainThread();

        // 获取异步的 webrequest
        async UniTask<string> GetTextAsync(UnityWebRequest req)
        {
            var op = await req.SendWebRequest();
            return op.downloadHandler.text;
        }

        var task1 = GetTextAsync(UnityWebRequest.Get("http://google.com"));
        var task2 = GetTextAsync(UnityWebRequest.Get("http://bing.com"));
        var task3 = GetTextAsync(UnityWebRequest.Get("http://yahoo.com"));

        // 构造一个async-wait，并通过元组语义轻松获取所有结果
        var (google, bing, yahoo) = await UniTask.WhenAll(task1, task2, task3);

        // WhenAll简写形式
        var (google2, bing2, yahoo2) = await (task1, task2, task3);

        // 返回一个异步值，或者你也可以使用`UniTask`(无结果), `UniTaskVoid`(协程，不可等待)
        return (asset as TextAsset)?.text ?? throw new InvalidOperationException("Asset not found");
    }


    IEnumerator FooCoroutineEnumerator()
    {
        yield return null;
    }

}
