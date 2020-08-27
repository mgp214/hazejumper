using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Compilation;
using UnityEditor;
using System;
using System.Linq;

[InitializeOnLoad]
public class CompilationSteps {
	private static DateTime startTime;

	static CompilationSteps() {
		CompilationPipeline.compilationStarted += CompilationPipeline_compilationStarted; ;
		CompilationPipeline.compilationFinished += CompilationPipeline_compilationFinished; ;
	}

	private static void CompilationPipeline_compilationFinished(object obj) {
		var duration = DateTime.Now - startTime;
		var currentVersion = PlayerSettings.bundleVersion;
		var versionSegments = currentVersion.Split('.').ToList();
		var build = int.Parse(versionSegments.Last()) + 1;
		versionSegments.Remove(versionSegments.Last());
		versionSegments.Add(build.ToString());
		var nextVersion = string.Join(".", versionSegments);
		PlayerSettings.bundleVersion = nextVersion;
		AssetDatabase.SaveAssets();

		Debug.Log($"Build {nextVersion}: compilation took {duration:mm\\:ss\\.ffff}");
	}

	private static void CompilationPipeline_compilationStarted(object obj) {
		startTime = DateTime.Now;
	}
}