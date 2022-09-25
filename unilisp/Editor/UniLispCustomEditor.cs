using System.Collections;
using System.Collections.Generic;
using System.IO;
using UniLisp;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DefaultAsset))]
public class UniLispCustomEditor : Editor
{
    string m_REPLText;

    public override void OnInspectorGUI()
    {
        target.hideFlags = HideFlags.None;
        var extension = Path.GetExtension(AssetDatabase.GetAssetPath(target));
        if (string.IsNullOrEmpty(extension) == false && extension.ToLower() == ".lisp")
        {
            LispInspectorGUI();
        }
    }

    private void ResetContext()
    {
        UniLispGlobalContext.Reset();
    }

    private void ExecuteScript()
    {
        var path = AssetDatabase.GetAssetPath(target);
        var content = File.ReadAllText(path);
        Execute(content);
    }

    private void Execute(string content)
    {
        try
        {
            var inport = new InPort(new StringReader(content));
            var expr = LispValue.Nil;
            while ((expr = UniLispGlobalContext.get.Parse(inport)).type != LispType.EoF)
            {
                var val = UniLispGlobalContext.get.Eval(expr);
                var result = LispValue.Stringigy(val);
                Debug.Log(result);
            }
        }
        catch (LispSyntaxException e)
        {
            Debug.LogError($"LispSyntax error: {e}");
        }
        catch (LispRuntimeException e)
        {
            Debug.LogError($"LispRuntimeerror: {e}");
        }
        catch (System.Exception e)
        {
            throw e;
        }
    }

    private void LispInspectorGUI()
    {
        if (GUILayout.Button("Reset Context"))
        {
            ResetContext();
        }
        if (GUILayout.Button("Execute Script"))
        {
            ExecuteScript();
        }

        GUILayout.Label("REPL");
        m_REPLText = GUILayout.TextArea(m_REPLText, GUILayout.Height(300));
        if (GUILayout.Button("Execute Command"))
        {
            var toExecute = m_REPLText;
            var editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (editor != null && editor.hasSelection)
            {
                toExecute = editor.SelectedText;
            }
            Execute(toExecute);
        }
    }
}
