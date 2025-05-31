using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.VisualScripting;
using WebrequestVisualScriptingNodes; // Your namespace
using STYLY.Http.Service; // For HttpResponse
using UnityEngine.Networking; // For UnityWebRequest related stuff if needed for mocks

public class PostNodesTests
{
    private GameObject eventSystemGO;
    private GameObject graphGameObject;
    private ScriptMachine scriptMachine;

    // A mock server URL for testing. Postman Echo is a good option.
    private const string TestServerUrl = "https://postman-echo.com/post";

    [SetUp]
    public void SetUp()
    {
        // Basic setup for Visual Scripting tests if needed.
        // This might need to be more comprehensive depending on how Visual Scripting initializes.
        // Create a GameObject to host the ScriptMachine
        graphGameObject = new GameObject("TestGraphHost");
        scriptMachine = graphGameObject.AddComponent<ScriptMachine>();

        // Ensure an EventSystem exists if your nodes interact with UI or need event processing
        // This is often a source of issues if missing in test environments
        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up GameObjects created for the test
        if (graphGameObject != null)
        {
            GameObject.DestroyImmediate(graphGameObject);
        }
        if (eventSystemGO != null)
        {
            GameObject.DestroyImmediate(eventSystemGO);
        }
    }

    // Helper to create and run a graph for a specific node type
    // This is a simplified version. Actual graph setup can be complex.
    private IEnumerator RunNodeTest(System.Type nodeType, ValueInput urlPort, string urlValue, ValueInput dataPort, object dataValue, ValueOutput resultPort, System.Action<object, bool, string> callback)
    {
        // This is a placeholder for how you might set up and run a VS graph in a test.
        // In a real scenario, you'd create a ScriptGraphAsset, add your node,
        // connect ports, and then trigger it.
        // For now, we'll simulate the node's direct behavior as much as possible,
        // or assume a simplified graph execution.

        // Create an instance of the node
        var node = System.Activator.CreateInstance(nodeType) as Unit;
        if (node == null)
        {
            callback(null, false, "Failed to create node instance.");
            yield break;
        }

        // Simulate setting input values
        // This is highly simplified. In reality, you'd use flow.SetValue for ValueInputs.
        // We're directly invoking the 'Enter' coroutine which is an internal part of the node.
        // This bypasses some of the VS execution logic but can work for unit-level tests.

        // The 'Enter' method in your nodes is a coroutine.
        // To test it, you need to run it as a coroutine.
        // Graph flow for setting values (simplified)
        var flow = GraphFakeFlow.New(null);
        flow.SetValue(urlPort, urlValue);
        if (dataPort != null) // dataPort is null for GET nodes
        {
            flow.SetValue(dataPort, dataValue);
        }

        // Definition needs to be called to initialize ports
        node.GetType().GetMethod("Definition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(node, null);

        // Get the Enter method
        var enterMethod = node.GetType().GetMethod("Enter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (enterMethod == null)
        {
            callback(null, false, "Enter method not found on node.");
            yield break;
        }

        // Start the coroutine
        var enumerator = (IEnumerator)enterMethod.Invoke(node, new object[] { flow });
        float timeout = Time.realtimeSinceStartup + 20f; // 20 seconds timeout

        bool successTriggered = false;
        bool errorTriggered = false;
        string errorMessage = "";

        // Hook into output triggers (simplified)
        // In a real graph, these would be connected to other nodes.
        // Here, we're just checking if they were invoked.
        // This part is tricky without a full VS graph execution environment.
        // We'll rely on the enumerator yielding the correct ControlOutputs.

        ControlOutput successOutput = (ControlOutput)node.GetType().GetField("outputTrigger").GetValue(node);
        ControlOutput errorOutput = (ControlOutput)node.GetType().GetField("outputTrigger_Error").GetValue(node);

        while (enumerator.MoveNext())
        {
            if (Time.realtimeSinceStartup > timeout)
            {
                callback(null, false, "Test timed out.");
                yield break;
            }
            if (enumerator.Current == successOutput) successTriggered = true;
            if (enumerator.Current == errorOutput) errorTriggered = true;

            // If the coroutine yields a ControlOutput, and we're waiting for it, break or continue
            if (successTriggered || errorTriggered) break;
            yield return null; // Wait for the next frame
        }

        // Wait a frame for async operations inside Enter to potentially complete
        yield return null;

        if (successTriggered)
        {
            var resultValue = flow.GetValue(resultPort);
            callback(resultValue, true, string.Empty);
        }
        else if (errorTriggered)
        {
             var errorResult = flow.GetValue(resultPort); // Error message is often put in result for these nodes
            callback(errorResult, false, "Node triggered error output. Message: " + errorResult);
        }
        else
        {
            callback(null, false, "Node did not trigger success or error output within the coroutine's execution path.");
        }
    }


    [UnityTest]
    public IEnumerator PostTextNode_SendsTextData_ReceivesResponse()
    {
        var postTextNode = new PostText();
        // Call Definition to initialize ports
        postTextNode.GetType().GetMethod("Definition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(postTextNode, null);

        string testData = "Hello Visual Scripting";
        bool testComplete = false;
        string responseText = null;
        bool success = false;

        yield return RunNodeTest(
            typeof(PostText),
            postTextNode.URL, // ValueInput port for URL
            TestServerUrl,
            postTextNode.TextData, // ValueInput port for TextData
            testData,
            postTextNode.result, // ValueOutput port for result
            (result, s, errorMsg) =>
            {
                responseText = result as string;
                success = s;
                if (!s) Debug.LogError("Test Error: " + errorMsg);
                testComplete = true;
            }
        );

        yield return new WaitUntil(() => testComplete);

        Assert.IsTrue(success, "PostText node did not trigger success output.");
        Assert.IsNotNull(responseText, "Response text should not be null.");
        Debug.Log("PostText Response: " + responseText);
        // Postman Echo returns a JSON response that includes the data sent.
        Assert.IsTrue(responseText.Contains(testData), "Response should contain the sent data.");
    }

    [UnityTest]
    public IEnumerator PostTextureNode_SendsTextureData_ReceivesResponse()
    {
        var postTextureNode = new PostTexture();
        // Call Definition to initialize ports
        postTextureNode.GetType().GetMethod("Definition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(postTextureNode, null);

        // Create a dummy Texture2D for testing
        Texture2D testTexture = new Texture2D(2, 2);
        // Fill with some color, ensure it's readable
        testTexture.SetPixel(0, 0, Color.red);
        testTexture.SetPixel(0, 1, Color.green);
        testTexture.SetPixel(1, 0, Color.blue);
        testTexture.SetPixel(1, 1, Color.white);
        testTexture.Apply(); // Apply changes to the texture

        bool testComplete = false;
        string responseText = null;
        bool success = false;

        yield return RunNodeTest(
            typeof(PostTexture),
            postTextureNode.URL,
            TestServerUrl,
            postTextureNode.TextureData,
            testTexture,
            postTextureNode.result,
            (result, s, errorMsg) =>
            {
                responseText = result as string;
                success = s;
                if (!s) Debug.LogError("Test Error: " + errorMsg);
                testComplete = true;
            }
        );

        yield return new WaitUntil(() => testComplete);

        // Clean up the created texture
        Object.DestroyImmediate(testTexture);

        Assert.IsTrue(success, "PostTexture node did not trigger success output.");
        Assert.IsNotNull(responseText, "Response text should not be null for PostTexture.");
        Debug.Log("PostTexture Response: " + responseText);
        // Postman echo for binary data might return info about the file/data in JSON format.
        // Check for a common indicator of successful binary data reception.
        Assert.IsTrue(responseText.Contains("\"content-type\":\"image/png\"") || responseText.Contains("data:image/png;base64"), "Response should indicate PNG data was processed or include content type header.");
    }

    [UnityTest]
    public IEnumerator PostJsonNode_SendsJsonData_ReceivesResponse()
    {
        var postJsonNode = new PostJson();
        // Call Definition to initialize ports
        postJsonNode.GetType().GetMethod("Definition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(postJsonNode, null);

        string testJson = "{\"name\":\"VisualScriptingTest\", \"value\":123}"; // Escaped JSON
        bool testComplete = false;
        string responseText = null;
        bool success = false;

        yield return RunNodeTest(
            typeof(PostJson),
            postJsonNode.URL,
            TestServerUrl,
            postJsonNode.JsonData,
            testJson,
            postJsonNode.result,
            (result, s, errorMsg) =>
            {
                responseText = result as string;
                success = s;
                if (!s) Debug.LogError("Test Error: " + errorMsg);
                testComplete = true;
            }
        );

        yield return new WaitUntil(() => testComplete);

        Assert.IsTrue(success, "PostJson node did not trigger success output.");
        Assert.IsNotNull(responseText, "Response text should not be null for PostJson.");
        Debug.Log("PostJson Response: " + responseText);
        // Postman Echo returns a JSON response that includes the json data sent.
        // Need to parse the expected JSON from the actual JSON.
        Assert.IsTrue(responseText.Contains("\"name\":\"VisualScriptingTest\"") && responseText.Contains("\"value\":123"), "Response should contain the sent JSON data.");
    }
}

// Minimal fake Flow class for testing Units directly
// This is a simplified mock. A more robust solution might involve a proper mocking framework
// or more detailed simulation of Visual Scripting's Flow class.
public class GraphFakeFlow : Flow
{
    private readonly System.Collections.Generic.Dictionary<ValueInput, object> values = new System.Collections.Generic.Dictionary<ValueInput, object>();
    private readonly System.Collections.Generic.Dictionary<ValueOutput, System.Func<GraphFakeFlow, object>> functions = new System.Collections.Generic.Dictionary<ValueOutput, System.Func<GraphFakeFlow, object>>();

    // Private constructor to control instantiation, use New() method
    private GraphFakeFlow(GraphReference reference) : base(reference) {}

    public static GraphFakeFlow New(GraphReference reference)
    {
        // Visual Scripting's Flow class typically requires a GraphReference.
        // For unit tests, we might not have a full graph, so a dummy reference is needed.
        // Creating a ScriptableObject solely for a GraphPointer isn't ideal without more setup.
        // If 'null' is problematic, a mock or a minimal ScriptableObject instance might be required.
        // For now, assuming 'null' might work for basic tests or that the constructor handles it.
        return new GraphFakeFlow(reference);
    }

    public override bool isInspected { get { return false; } }
    public override bool isSerializationRoot { get { return false; } }

    public override object GetValue(ValueInput port)
    {
        if (values.TryGetValue(port, out var value))
        {
            return value;
        }
        // Fallback to default if not set, or handle as error
        return port.hasDefaultValue ? port.defaultValue : System.Type.Missing;
    }
     public override object GetValue(ValueOutput port)
    {
        if (functions.TryGetValue(port, out var func))
        {
            return func(this);
        }
        // Fallback or error
        return System.Type.Missing;
    }


    public void SetValue(ValueInput port, object value)
    {
        values[port] = value;
    }

    public void SetFunction(ValueOutput port, System.Func<GraphFakeFlow, object> func)
    {
        functions[port] = func;
    }


    // Implement other abstract members with minimal stubs if they are called by the nodes
    public override void Branch() { /* Stub */ }
    public override void Run(ControlOutput output) { /* Stub - or track invocation */ }
    public override Coroutine StartCoroutine(IEnumerator coroutine)
    {
        // In Unity editor tests, MonoBehaviour.StartCoroutine is not directly available
        // unless the test class inherits from MonoBehaviourTest.
        // For simple coroutines, you might be able to iterate them manually in the test
        // or use a helper that can run coroutines.
        // If your nodes use `flow.StartCoroutine`, this needs to be handled.
        // For now, let's assume tests will manage coroutine execution directly.
        Debug.LogWarning("GraphFakeFlow.StartCoroutine called. Direct coroutine management in test is preferred.");
        return null; // Or a mock Coroutine if necessary
    }
    public override void StopCoroutine(Coroutine coroutine) { /* Stub */ }
    public override bool IsCoroutineRunning(Coroutine coroutine) { return false; /* Stub */ }
    public override T GetVariable<T>(string name) { return default(T); /* Stub */ }
    public override void SetVariable<T>(string name, T value) { /* Stub */ }
    public override bool HasVariable(string name) { return false; /* Stub */ }

    // Required by Flow but might not be used by simple units
    protected override void Configure(GraphReference reference) { /* Stub for base class */ }
    public override bool CanGetValue(ValueInput port) { return values.ContainsKey(port); }
    public override bool CanGetValue(ValueOutput port) { return functions.ContainsKey(port); }
    public override void Dispose() { /* Stub */ }
    public override void EnterLoop(ControlLoop loop) { /* Stub */ }
    public override void ExitLoop(ControlLoop loop) { /* Stub */ }
    public override bool IsLoopBroken(ControlLoop loop) { return false; /* Stub */ }
    public override void BreakLoop(ControlLoop loop) { /* Stub */ }
    public override bool IsListening(GraphReference reference, EventHook hook) { return false; /* Stub */ }
    public override void StartListening(GraphReference reference, EventHook hook) { /* Stub */ }
    public override void StopListening(GraphReference reference, EventHook hook) { /* Stub */ }
    public override void Trigger(EventHook hook, ScriptEventArgs args) { /* Stub */ }
    public override bool IsActive(Unit unit) {return true; /* Stub */}
    public override void EnsureActive(Unit unit) { /* Stub */ }
    public override void EnsureInactive(Unit unit) { /* Stub */ }

}
