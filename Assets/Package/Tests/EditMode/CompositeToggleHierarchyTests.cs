using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mobcast.Coffee.Toggles.Tests
{
	public class CompositeToggleHierarchyTests
	{
		Scene scene;
		GameObject target;
		MethodInfo hasToggle;

		[SetUp]
		public void SetUp()
		{
			var type = Type.GetType("Mobcast.Coffee.Toggles.CompositeToggleHierarchy, CompositeToggle.Editor.asmdef", true);
			hasToggle = type.GetMethod("HasToggle", BindingFlags.Static | BindingFlags.NonPublic);
			scene = EditorSceneManager.NewPreviewScene();
			target = new GameObject("Unmodified object name");
			SceneManager.MoveGameObjectToScene(target, scene);
		}

		[TearDown]
		public void TearDown()
		{
			EditorSceneManager.ClosePreviewScene(scene);
		}

		bool HasToggle(GameObject value) => (bool)hasToggle.Invoke(null, new object[] { value });

		[Test]
		public void OrdinaryObjectHasNoBadge() => Assert.That(HasToggle(target), Is.False);

		[Test]
		public void AttachedToggleHasBadgeWithoutRenamingOrDirtyingScene()
		{
			target.AddComponent<CompositeToggle>();
			bool dirty = scene.isDirty;
			Assert.That(HasToggle(target), Is.True);
			Assert.That(target.name, Is.EqualTo("Unmodified object name"));
			Assert.That(scene.isDirty, Is.EqualTo(dirty));
		}

		[Test]
		public void DisabledToggleStillHasBadge()
		{
			target.AddComponent<CompositeToggle>().enabled = false;
			Assert.That(HasToggle(target), Is.True);
		}

		[Test]
		public void InactiveObjectStillHasBadge()
		{
			target.AddComponent<CompositeToggle>();
			target.SetActive(false);
			Assert.That(HasToggle(target), Is.True);
		}

		[Test]
		public void ChildToggleDoesNotMarkParent()
		{
			var child = new GameObject("Child");
			SceneManager.MoveGameObjectToScene(child, scene);
			child.transform.SetParent(target.transform);
			child.AddComponent<CompositeToggle>();
			Assert.That(HasToggle(target), Is.False);
			Assert.That(HasToggle(child), Is.True);
		}

		[Test]
		public void RemovedComponentDoesNotLeaveStaleBadge()
		{
			var toggle = target.AddComponent<CompositeToggle>();
			Assert.That(HasToggle(target), Is.True);
			UnityEngine.Object.DestroyImmediate(toggle);
			Assert.That(HasToggle(target), Is.False);
		}

		[Test]
		public void NullAndDestroyedObjectsHaveNoBadge()
		{
			Assert.That(HasToggle(null), Is.False);
			UnityEngine.Object.DestroyImmediate(target);
			Assert.That(HasToggle(target), Is.False);
		}
	}
}
