//#define VERBOSE_LOGGING
#if !VRC_CLIENT
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

#if VERBOSE_LOGGING
using System.Text;
#endif

public class InjectStereoVariants : IPreprocessShaders
{
    public int callbackOrder => 1024;

    private readonly ShaderKeyword _unitySinglePassStereoKeyword;
    private readonly ShaderKeyword _stereoInstancingKeyword;

    public InjectStereoVariants()
    {
        _unitySinglePassStereoKeyword = new ShaderKeyword("UNITY_SINGLE_PASS_STEREO");
        _stereoInstancingKeyword = new ShaderKeyword("STEREO_INSTANCING_ON");
    }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if(EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            return;
        }

        List<ShaderCompilerData> newVariants = new List<ShaderCompilerData>();
        foreach(ShaderCompilerData variant in data)
        {
            ShaderCompilerData newVariant = variant;
            ShaderKeywordSet shaderKeywordSet = variant.shaderKeywordSet;
            if(shaderKeywordSet.IsEnabled(_unitySinglePassStereoKeyword))
            {
                shaderKeywordSet.Disable(_unitySinglePassStereoKeyword);
                shaderKeywordSet.Enable(_stereoInstancingKeyword);
                newVariant.shaderKeywordSet = shaderKeywordSet;
                newVariants.Add(newVariant);
                continue;
            }

            // ReSharper disable once InvertIf
            if(shaderKeywordSet.IsEnabled(_stereoInstancingKeyword))
            {
                shaderKeywordSet.Enable(_unitySinglePassStereoKeyword);
                shaderKeywordSet.Disable(_stereoInstancingKeyword);
                newVariant.shaderKeywordSet = shaderKeywordSet;
                newVariants.Add(newVariant);
                // ReSharper disable once RedundantJumpStatement
                continue;
            }
        }

        foreach(ShaderCompilerData entry in newVariants)
        {
            data.Add(entry);
        }

        #if VERBOSE_LOGGING
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"Pass Name: {snippet.passName} Pass Type: {snippet.passType} Shader Type: {snippet.shaderType}");
        foreach(ShaderCompilerData entry in data)
        {
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            ShaderKeyword[] shaderKeywords = entry.shaderKeywordSet.GetShaderKeywords();
            foreach(ShaderKeyword keyword in shaderKeywords)
            {
                stringBuilder.Append(ShaderKeyword.GetKeywordName(shader, keyword));
                stringBuilder.Append(" ");
            }

            stringBuilder.AppendLine();
        }

        Debug.LogWarning(stringBuilder.ToString());
        #endif
    }
}
#endif
