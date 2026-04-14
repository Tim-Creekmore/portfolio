using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public class TestServer : MonoBehaviour
{
    [SerializeField] int port = 13000;

    TcpListener _listener;
    Thread _listenThread;
    volatile bool _running;

    readonly ConcurrentQueue<(TcpClient client, string cmd)> _incomingCommands =
        new ConcurrentQueue<(TcpClient, string)>();

    static readonly Queue<string> _logBuffer = new Queue<string>();
    const int LOG_BUFFER_SIZE = 200;

    float _fps;
    float _fpsTimer;
    int _fpsFrames;

    void Awake()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        Destroy(this);
        return;
#endif
        Application.logMessageReceived += OnLogMessage;
    }

    void Start()
    {
        _running = true;
        _listenThread = new Thread(ListenLoop) { IsBackground = true };
        _listenThread.Start();
        Debug.Log($"[TestServer] Listening on port {port}");
    }

    void OnDestroy()
    {
        _running = false;
        Application.logMessageReceived -= OnLogMessage;
        try { _listener?.Stop(); } catch { }
    }

    static void OnLogMessage(string message, string stackTrace, LogType type)
    {
        string prefix = type == LogType.Error || type == LogType.Exception ? "ERROR"
                      : type == LogType.Warning ? "WARN" : "INFO";
        string entry = $"[{prefix}] {message}";
        lock (_logBuffer)
        {
            _logBuffer.Enqueue(entry);
            while (_logBuffer.Count > LOG_BUFFER_SIZE) _logBuffer.Dequeue();
        }
    }

    void Update()
    {
        _fpsFrames++;
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= 1f)
        {
            _fps = _fpsFrames / _fpsTimer;
            _fpsFrames = 0;
            _fpsTimer = 0f;
        }

        while (_incomingCommands.TryDequeue(out var pair))
            HandleCommand(pair.client, pair.cmd);
    }

    void ListenLoop()
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            while (_running)
            {
                if (!_listener.Pending()) { Thread.Sleep(50); continue; }
                var client = _listener.AcceptTcpClient();
                new Thread(() => ClientLoop(client)) { IsBackground = true }.Start();
            }
        }
        catch (Exception e)
        {
            if (_running) Debug.LogError($"[TestServer] Listen error: {e.Message}");
        }
    }

    void ClientLoop(TcpClient client)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (_running && client.Connected)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    line = line.Trim();
                    if (line.Length == 0) continue;
                    _incomingCommands.Enqueue((client, line));
                }
            }
        }
        catch (Exception e)
        {
            if (_running) Debug.Log($"[TestServer] Client disconnected: {e.Message}");
        }
    }

    void HandleCommand(TcpClient client, string raw)
    {
        string response;
        try
        {
            string[] parts = raw.Split(new[] { ' ' }, 2);
            string cmd = parts[0].ToUpperInvariant();
            string arg = parts.Length > 1 ? parts[1].Trim() : "";

            switch (cmd)
            {
                case "PING":       response = "{\"pong\":true}"; break;
                case "SCENE":      response = CmdScene(); break;
                case "FIND":       response = CmdFind(arg); break;
                case "SCREENSHOT": CmdScreenshot(client, arg); return;
                case "STATS":      response = CmdStats(); break;
                case "HIERARCHY":  response = CmdHierarchy(arg); break;
                case "CHILDREN":   response = CmdChildren(arg); break;
                case "INSPECT":    response = CmdInspect(arg); break;
                case "MESHINFO":   response = CmdMeshInfo(arg); break;
                case "LOG":        response = CmdLog(arg); break;
                case "COUNT":      response = CmdCount(arg); break;
                case "TERRAIN":    response = CmdTerrain(arg); break;
                case "HELP":       response = CmdHelp(); break;
                default:
                    response = $"{{\"error\":\"unknown command: {Esc(cmd)}. Send HELP for list.\"}}";
                    break;
            }
        }
        catch (Exception e)
        {
            response = $"{{\"error\":\"{Esc(e.Message)}\"}}";
        }

        Send(client, response);
    }

    // ── Commands ────────────────────────────────────────────────────────────

    string CmdScene()
    {
        string scene = SceneManager.GetActiveScene().name;
        int rootCount = SceneManager.GetActiveScene().rootCount;
        int totalGO = FindObjectsOfType<Transform>(true).Length;
        return $"{{\"scene\":\"{Esc(scene)}\",\"rootObjects\":{rootCount},\"totalObjects\":{totalGO}}}";
    }

    string CmdFind(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "{\"found\":false,\"error\":\"no name provided\"}";

        var go = GameObject.Find(name);
        if (go == null)
            return "{\"found\":false,\"name\":\"" + Esc(name) + "\"}";

        return "{\"found\":true," + GoJson(go) + "}";
    }

    string CmdStats()
    {
        var cam = Camera.main;
        string camPos = "null";
        string camRot = "null";
        if (cam != null)
        {
            var p = cam.transform.position;
            var r = cam.transform.eulerAngles;
            camPos = $"[{F(p.x)},{F(p.y)},{F(p.z)}]";
            camRot = $"[{F(r.x)},{F(r.y)},{F(r.z)}]";
        }

        long totalMem = Profiler.GetTotalAllocatedMemoryLong();
        long texMem = Profiler.GetAllocatedMemoryForGraphicsDriver();
        int totalGO = FindObjectsOfType<Transform>(true).Length;
        int meshRenderers = FindObjectsOfType<MeshRenderer>(true).Length;
        int meshFilters = FindObjectsOfType<MeshFilter>(true).Length;

        return "{" +
            $"\"fps\":{F(_fps)}," +
            $"\"totalGameObjects\":{totalGO}," +
            $"\"meshRenderers\":{meshRenderers}," +
            $"\"meshFilters\":{meshFilters}," +
            $"\"totalMemoryMB\":{F(totalMem / (1024f * 1024f))}," +
            $"\"gpuMemoryMB\":{F(texMem / (1024f * 1024f))}," +
            $"\"cameraPosition\":{camPos}," +
            $"\"cameraRotation\":{camRot}," +
            $"\"time\":{F(Time.time)}," +
            $"\"deltaTime\":{F(Time.deltaTime)}" +
            "}";
    }

    string CmdHierarchy(string arg)
    {
        int maxDepth = 1;
        if (!string.IsNullOrEmpty(arg) && int.TryParse(arg, out int d)) maxDepth = d;

        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        var sb = new StringBuilder();
        sb.Append("{\"roots\":[");
        for (int i = 0; i < roots.Length; i++)
        {
            if (i > 0) sb.Append(",");
            AppendHierarchyNode(sb, roots[i].transform, 0, maxDepth);
        }
        sb.Append("]}");
        return sb.ToString();
    }

    void AppendHierarchyNode(StringBuilder sb, Transform t, int depth, int maxDepth)
    {
        sb.Append("{\"name\":\"").Append(Esc(t.name)).Append("\"");
        sb.Append(",\"active\":").Append(Bool(t.gameObject.activeInHierarchy));
        sb.Append(",\"childCount\":").Append(t.childCount);

        var mf = t.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            sb.Append(",\"verts\":").Append(mf.sharedMesh.vertexCount);

        var mr = t.GetComponent<MeshRenderer>();
        if (mr != null && mr.sharedMaterial != null)
            sb.Append(",\"material\":\"").Append(Esc(mr.sharedMaterial.name)).Append("\"");

        if (depth < maxDepth && t.childCount > 0)
        {
            sb.Append(",\"children\":[");
            for (int i = 0; i < t.childCount; i++)
            {
                if (i > 0) sb.Append(",");
                AppendHierarchyNode(sb, t.GetChild(i), depth + 1, maxDepth);
            }
            sb.Append("]");
        }

        sb.Append("}");
    }

    string CmdChildren(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "{\"error\":\"no name provided\"}";

        var go = GameObject.Find(name);
        if (go == null)
            return "{\"found\":false,\"name\":\"" + Esc(name) + "\"}";

        var sb = new StringBuilder();
        sb.Append("{\"parent\":\"").Append(Esc(go.name)).Append("\",\"children\":[");
        for (int i = 0; i < go.transform.childCount; i++)
        {
            if (i > 0) sb.Append(",");
            var child = go.transform.GetChild(i);
            sb.Append("{").Append(GoJson(child.gameObject)).Append("}");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    string CmdInspect(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "{\"error\":\"no name provided\"}";

        var go = GameObject.Find(name);
        if (go == null)
            return "{\"found\":false,\"name\":\"" + Esc(name) + "\"}";

        var sb = new StringBuilder();
        sb.Append("{\"name\":\"").Append(Esc(go.name)).Append("\"");
        sb.Append(",\"active\":").Append(Bool(go.activeInHierarchy));
        sb.Append(",\"layer\":").Append(go.layer);
        sb.Append(",\"tag\":\"").Append(Esc(go.tag)).Append("\"");

        var pos = go.transform.position;
        var rot = go.transform.eulerAngles;
        var scl = go.transform.lossyScale;
        sb.Append(",\"position\":[").Append(F(pos.x)).Append(",").Append(F(pos.y)).Append(",").Append(F(pos.z)).Append("]");
        sb.Append(",\"rotation\":[").Append(F(rot.x)).Append(",").Append(F(rot.y)).Append(",").Append(F(rot.z)).Append("]");
        sb.Append(",\"scale\":[").Append(F(scl.x)).Append(",").Append(F(scl.y)).Append(",").Append(F(scl.z)).Append("]");

        var components = go.GetComponents<Component>();
        sb.Append(",\"components\":[");
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null) continue;
            if (i > 0) sb.Append(",");
            sb.Append("\"").Append(Esc(components[i].GetType().Name)).Append("\"");
        }
        sb.Append("]");

        var mf = go.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var mesh = mf.sharedMesh;
            sb.Append(",\"mesh\":{");
            sb.Append("\"name\":\"").Append(Esc(mesh.name)).Append("\"");
            sb.Append(",\"vertices\":").Append(mesh.vertexCount);
            sb.Append(",\"triangles\":").Append(mesh.triangles.Length / 3);
            sb.Append(",\"subMeshes\":").Append(mesh.subMeshCount);
            var b = mesh.bounds;
            sb.Append(",\"boundsCenter\":[").Append(F(b.center.x)).Append(",").Append(F(b.center.y)).Append(",").Append(F(b.center.z)).Append("]");
            sb.Append(",\"boundsSize\":[").Append(F(b.size.x)).Append(",").Append(F(b.size.y)).Append(",").Append(F(b.size.z)).Append("]");
            sb.Append(",\"hasColors\":").Append(Bool(mesh.colors != null && mesh.colors.Length > 0));
            sb.Append(",\"hasNormals\":").Append(Bool(mesh.normals != null && mesh.normals.Length > 0));
            sb.Append(",\"hasUVs\":").Append(Bool(mesh.uv != null && mesh.uv.Length > 0));
            sb.Append("}");
        }

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            sb.Append(",\"renderer\":{");
            sb.Append("\"enabled\":").Append(Bool(mr.enabled));
            sb.Append(",\"shadowCasting\":\"").Append(mr.shadowCastingMode.ToString()).Append("\"");
            sb.Append(",\"receiveShadows\":").Append(Bool(mr.receiveShadows));
            if (mr.sharedMaterial != null)
            {
                sb.Append(",\"material\":\"").Append(Esc(mr.sharedMaterial.name)).Append("\"");
                sb.Append(",\"shader\":\"").Append(Esc(mr.sharedMaterial.shader.name)).Append("\"");
            }
            sb.Append("}");
        }

        sb.Append(",\"childCount\":").Append(go.transform.childCount);
        if (go.transform.parent != null)
            sb.Append(",\"parent\":\"").Append(Esc(go.transform.parent.name)).Append("\"");

        sb.Append("}");
        return sb.ToString();
    }

    string CmdMeshInfo(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "{\"error\":\"no name provided\"}";

        var go = GameObject.Find(name);
        if (go == null)
            return "{\"found\":false,\"name\":\"" + Esc(name) + "\"}";

        var filters = go.GetComponentsInChildren<MeshFilter>(true);
        var sb = new StringBuilder();
        sb.Append("{\"object\":\"").Append(Esc(go.name)).Append("\",\"meshes\":[");

        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh == null) continue;
            if (i > 0) sb.Append(",");
            var mesh = filters[i].sharedMesh;
            var mr2 = filters[i].GetComponent<MeshRenderer>();
            sb.Append("{");
            sb.Append("\"gameObject\":\"").Append(Esc(filters[i].gameObject.name)).Append("\"");
            sb.Append(",\"meshName\":\"").Append(Esc(mesh.name)).Append("\"");
            sb.Append(",\"vertices\":").Append(mesh.vertexCount);
            sb.Append(",\"triangles\":").Append(mesh.triangles.Length / 3);
            var b = mesh.bounds;
            sb.Append(",\"boundsSize\":[").Append(F(b.size.x)).Append(",").Append(F(b.size.y)).Append(",").Append(F(b.size.z)).Append("]");
            if (mr2 != null && mr2.sharedMaterial != null)
            {
                sb.Append(",\"material\":\"").Append(Esc(mr2.sharedMaterial.name)).Append("\"");
                sb.Append(",\"shader\":\"").Append(Esc(mr2.sharedMaterial.shader.name)).Append("\"");
            }
            sb.Append("}");
        }

        sb.Append("]}");
        return sb.ToString();
    }

    string CmdLog(string arg)
    {
        int count = 50;
        if (!string.IsNullOrEmpty(arg) && int.TryParse(arg, out int n)) count = n;

        string[] entries;
        lock (_logBuffer)
        {
            var arr = _logBuffer.ToArray();
            int start = Mathf.Max(0, arr.Length - count);
            entries = new string[arr.Length - start];
            Array.Copy(arr, start, entries, 0, entries.Length);
        }

        var sb = new StringBuilder();
        sb.Append("{\"count\":").Append(entries.Length).Append(",\"entries\":[");
        for (int i = 0; i < entries.Length; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append("\"").Append(Esc(entries[i])).Append("\"");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    string CmdCount(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return "{\"error\":\"no pattern provided\"}";

        var all = FindObjectsOfType<Transform>(true);
        int count = 0;
        var sb = new StringBuilder();
        sb.Append("{\"pattern\":\"").Append(Esc(pattern)).Append("\",\"matches\":[");

        bool isPrefix = pattern.EndsWith("*");
        string prefix = isPrefix ? pattern.Substring(0, pattern.Length - 1) : pattern;

        bool first = true;
        foreach (var t in all)
        {
            bool match = isPrefix ? t.name.StartsWith(prefix) : t.name == pattern;
            if (!match) continue;

            count++;
            if (count <= 50)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append("{\"name\":\"").Append(Esc(t.name)).Append("\"");
                var p = t.position;
                sb.Append(",\"pos\":[").Append(F(p.x)).Append(",").Append(F(p.y)).Append(",").Append(F(p.z)).Append("]");
                sb.Append("}");
            }
        }

        sb.Append("],\"total\":").Append(count).Append("}");
        return sb.ToString();
    }

    string CmdTerrain(string arg)
    {
        string[] coords = arg.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (coords.Length < 2 ||
            !float.TryParse(coords[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float fx) ||
            !float.TryParse(coords[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float fz))
            return "{\"error\":\"usage: TERRAIN x z\"}";

        float h = WorldData.HeightSmooth(fx, fz);
        var biome = WorldData.GetBiome(fx, fz);
        bool isPond = WorldData.IsPond(fx, fz);
        float pondDist = WorldData.PondSDF(fx, fz);

        return "{" +
            $"\"x\":{F(fx)},\"z\":{F(fz)}," +
            $"\"height\":{F(h)}," +
            $"\"biome\":\"{biome}\"," +
            $"\"isPond\":{Bool(isPond)}," +
            $"\"pondDistance\":{F(pondDist)}" +
            "}";
    }

    string CmdHelp()
    {
        return "{\"commands\":[" +
            "\"PING - check connection\"," +
            "\"SCENE - active scene info\"," +
            "\"STATS - FPS, memory, camera, object counts\"," +
            "\"FIND <name> - find GameObject by name\"," +
            "\"INSPECT <name> - detailed info (components, mesh, material, transform)\"," +
            "\"MESHINFO <name> - mesh stats for object and children\"," +
            "\"CHILDREN <name> - list children of a GameObject\"," +
            "\"HIERARCHY [depth] - scene hierarchy tree (default depth=1)\"," +
            "\"COUNT <pattern> - count objects matching name (use * for prefix match)\"," +
            "\"LOG [n] - last N log messages (default 50)\"," +
            "\"TERRAIN x z - terrain height and biome at position\"," +
            "\"SCREENSHOT [path] - capture screenshot\"," +
            "\"HELP - this list\"" +
            "]}";
    }

    // ── Screenshot ──────────────────────────────────────────────────────────

    void CmdScreenshot(TcpClient client, string arg)
    {
        string path = string.IsNullOrEmpty(arg) ? "screenshot.png" : arg;
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        ScreenCapture.CaptureScreenshot(path);
        StartCoroutine(WaitForScreenshot(client, path));
    }

    System.Collections.IEnumerator WaitForScreenshot(TcpClient client, string path)
    {
        string fullPath = Path.IsPathRooted(path) ? path
            : Path.Combine(Application.dataPath, "..", path);
        fullPath = Path.GetFullPath(fullPath);

        float timeout = 5f;
        while (timeout > 0 && !File.Exists(fullPath))
        {
            yield return new WaitForSeconds(0.2f);
            timeout -= 0.2f;
        }

        if (File.Exists(fullPath))
        {
            long size = new FileInfo(fullPath).Length;
            Send(client, $"{{\"saved\":true,\"path\":\"{Esc(fullPath)}\",\"size\":{size}}}");
        }
        else
        {
            Send(client, $"{{\"saved\":false,\"error\":\"timeout waiting for screenshot\"}}");
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    string GoJson(GameObject go)
    {
        var p = go.transform.position;
        return $"\"name\":\"{Esc(go.name)}\",\"enabled\":{Bool(go.activeInHierarchy)}," +
               $"\"x\":{F(p.x)},\"y\":{F(p.y)},\"z\":{F(p.z)}";
    }

    void Send(TcpClient client, string json)
    {
        try
        {
            if (!client.Connected) return;
            byte[] data = Encoding.UTF8.GetBytes(json + "\n");
            client.GetStream().Write(data, 0, data.Length);
            client.GetStream().Flush();
        }
        catch { }
    }

    static string F(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
    static string Bool(bool v) => v ? "true" : "false";
    static string Esc(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
}
