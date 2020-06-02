using System;
using System.Collections.Generic;
using threeDtbd.Workflow.PackageManagement;
using UnityEngine;

namespace threeDtbd.Workflow
{
    [CreateAssetMenu(fileName = "New Project Workflow", menuName = "3D TBD/Workflow/Project")]
    public class ProjectWorkflow : ScriptableObject
    {
        public List<WorkflowStage> stages = new List<WorkflowStage>();
        public List<bool> expandPackageListInGUI = new List<bool>();

        internal bool ContainsStage(WorkflowStage stage)
        {
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i].name == stage.name) return true;
            }
            return false;
        }
    }
}