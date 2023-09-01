#if !VRC_CLIENT
using VRC.SDKBase.Editor.Validation;
using VRC.SDKBase.Validation.Performance.Stats;

namespace VRC.SDKBase.Validation.Performance
{
    public static class SDKPerformanceDisplay
    {
        public static void GetSDKPerformanceInfoText(
            AvatarPerformanceStats perfStats,
            AvatarPerformanceCategory perfCategory,
            out string text,
            out PerformanceInfoDisplayLevel displayLevel
        )
        {
            text = "";
            displayLevel = PerformanceInfoDisplayLevel.None;
            bool isMobilePlatform = ValidationEditorHelpers.IsMobilePlatform();

            PerformanceRating rating = perfStats.GetPerformanceRatingForCategory(perfCategory);
            switch(perfCategory)
            {
                case AvatarPerformanceCategory.Overall:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Info;
                            text = string.Format("Overall Performance: {0}", AvatarPerformanceStats.GetPerformanceRatingDisplayName(rating));
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Overall Performance: {0} - This avatar may not perform well on many systems." +
                                " See additional warnings for suggestions on how to improve performance. Click 'Avatar Optimization Tips' below for more information.",
                                AvatarPerformanceStats.GetPerformanceRatingDisplayName(rating)
                            );

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            if(ValidationEditorHelpers.IsMobilePlatform())
                            {
                                text = string.Format(
                                    "Overall Performance: {0} - This avatar does not meet minimum performance requirements for VRChat. " +
                                    "It will be blocked by default on VRChat for Quest, and will not show unless a user chooses to show your avatar." +
                                    " See additional warnings for suggestions on how to improve performance. Click 'Avatar Optimization Tips' below for more information.",
                                    AvatarPerformanceStats.GetPerformanceRatingDisplayName(rating));
                            }
                            else
                            {
                                text = string.Format(
                                    "Overall Performance: {0} - This avatar does not meet minimum performance requirements for VRChat. " +
                                    "It may be blocked by users depending on their Performance settings." +
                                    " See additional warnings for suggestions on how to improve performance. Click 'Avatar Optimization Tips' below for more information.",
                                    AvatarPerformanceStats.GetPerformanceRatingDisplayName(rating));
                            }

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.PolyCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Info;
                            text = string.Format("Polygons: {0}", perfStats.polyCount);
                            break;
                        }
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Info;
                            text = string.Format("Polygons: {0} (Recommended: {1})", perfStats.polyCount, AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).polyCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Polygons: {0} - Please try to reduce your avatar poly count to less than {1} (Recommended: {2})",
                                perfStats.polyCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Good, isMobilePlatform).polyCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).polyCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Polygons: {0} - This avatar has too many polygons. " +
                                (ValidationEditorHelpers.IsMobilePlatform()
                                    ? "It will be blocked by default on VRChat for Quest, and will not show unless a user chooses to show your avatar."
                                    : "It may be blocked by users depending on their Performance settings.") +
                                " It should have less than {1}. VRChat recommends having less than {2}.",
                                perfStats.polyCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).polyCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).polyCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.AABB:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Bounding box (AABB) size: {0}", perfStats.aabb.GetValueOrDefault().size.ToString());
                            break;
                        }
                        case PerformanceRating.Good:
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Bounding box (AABB) size: {0} (Recommended: {1})",
                                perfStats.aabb.GetValueOrDefault().size.ToString(),
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).aabb.size.ToString());

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "This avatar's bounding box (AABB) is too large on at least one axis. Current size: {0}, Maximum size: {1}",
                                perfStats.aabb.GetValueOrDefault().size.ToString(),
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).aabb.size.ToString());

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.SkinnedMeshCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Skinned Mesh Renderers: {0}", perfStats.skinnedMeshCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Skinned Mesh Renderers: {0} (Recommended: {1}) - Combine multiple skinned meshes for optimal performance.",
                                perfStats.skinnedMeshCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).skinnedMeshCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Skinned Mesh Renderers: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many skinned meshes." +
                                " Combine multiple skinned meshes for optimal performance.",
                                perfStats.skinnedMeshCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).skinnedMeshCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).skinnedMeshCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.MeshCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Mesh Renderers: {0}", perfStats.meshCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Mesh Renderers: {0} (Recommended: {1}) - Combine multiple meshes for optimal performance.",
                                perfStats.meshCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).meshCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Mesh Renderers: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many meshes. Combine multiple meshes for optimal performance.",
                                perfStats.meshCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).meshCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).meshCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.MaterialCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Material Slots: {0}", perfStats.materialCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Material Slots: {0} (Recommended: {1}) - Combine materials and atlas textures for optimal performance.",
                                perfStats.materialCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).materialCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Material Slots: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many materials. Combine materials and atlas textures for optimal performance.",
                                perfStats.materialCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).materialCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).materialCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.AnimatorCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Animator Count: {0}", perfStats.animatorCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Animator Count: {0} (Recommended: {1}) - Avoid using extra Animators for optimal performance.",
                                perfStats.animatorCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).animatorCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Animator Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many Animators. Avoid using extra Animators for optimal performance.",
                                perfStats.animatorCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).animatorCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).animatorCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.BoneCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Bones: {0}", perfStats.boneCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Bones: {0} (Recommended: {1}) - Reduce number of bones for optimal performance.",
                                perfStats.boneCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).boneCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Bones: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many bones. Reduce number of bones for optimal performance.",
                                perfStats.boneCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).boneCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).boneCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.LightCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Lights: {0}", perfStats.lightCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Lights: {0} (Recommended: {1}) - Avoid use of dynamic lights for optimal performance.",
                                perfStats.lightCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).lightCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Lights: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many dynamic lights. Avoid use of dynamic lights for optimal performance.",
                                perfStats.lightCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).lightCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).lightCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.ParticleSystemCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Particle Systems: {0}", perfStats.particleSystemCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Particle Systems: {0} (Recommended: {1}) - Reduce number of particle systems for better performance.",
                                perfStats.particleSystemCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).particleSystemCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Particle Systems: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many particle systems." +
                                " Reduce number of particle systems for better performance.",
                                perfStats.particleSystemCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).particleSystemCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).particleSystemCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.ParticleTotalCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Total Combined Max Particle Count: {0}", perfStats.particleTotalCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Total Combined Max Particle Count: {0} (Recommended: {1}) - Reduce 'Max Particles' across all particle systems for better performance.",
                                perfStats.particleTotalCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).particleTotalCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Total Combined Max Particle Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar uses too many particles." +
                                " Reduce 'Max Particles' across all particle systems for better performance.",
                                perfStats.particleTotalCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).particleTotalCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).particleTotalCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.ParticleMaxMeshPolyCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Mesh Particle Total Max Poly Count: {0}", perfStats.particleMaxMeshPolyCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Mesh Particle Total Max Poly Count: {0} (Recommended: {1}) - Reduce number of polygons in particle meshes, and reduce 'Max Particles' for better performance.",
                                perfStats.particleMaxMeshPolyCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).particleMaxMeshPolyCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Mesh Particle Total Max Poly Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar uses too many mesh particle polygons." +
                                " Reduce number of polygons in particle meshes, and reduce 'Max Particles' for better performance.",
                                perfStats.particleMaxMeshPolyCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).particleTotalCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).particleMaxMeshPolyCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.ParticleTrailsEnabled:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Particle Trails Enabled: {0}", perfStats.particleTrailsEnabled);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Particle Trails Enabled: {0} (Recommended: {1}) - Avoid particle trails for better performance.",
                                perfStats.particleTrailsEnabled,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).particleTrailsEnabled);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.ParticleCollisionEnabled:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Particle Collision Enabled: {0}", perfStats.particleCollisionEnabled);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Particle Collision Enabled: {0} (Recommended: {1}) - Avoid particle collision for better performance.",
                                perfStats.particleCollisionEnabled,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).particleCollisionEnabled);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.TrailRendererCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Trail Renderers: {0}", perfStats.trailRendererCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Trail Renderers: {0} (Recommended: {1}) - Reduce number of TrailRenderers for better performance.",
                                perfStats.trailRendererCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).trailRendererCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Trail Renderers: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many TrailRenderers. Reduce number of TrailRenderers for better performance.",
                                perfStats.trailRendererCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).trailRendererCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).trailRendererCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.LineRendererCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Line Renderers: {0}", perfStats.lineRendererCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Line Renderers: {0} (Recommended: {1}) - Reduce number of LineRenderers for better performance.",
                                perfStats.lineRendererCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).lineRendererCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Line Renderers: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many LineRenderers. Reduce number of LineRenderers for better performance.",
                                perfStats.lineRendererCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).lineRendererCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).lineRendererCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.DynamicBoneComponentCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Dynamic Bone Components: {0}", perfStats.dynamicBone?.componentCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Dynamic Bone Components: {0} (Recommended: {1}) - Reduce number of DynamicBone components for better performance.",
                                perfStats.dynamicBone?.componentCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).dynamicBone.componentCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Dynamic Bone Components: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many DynamicBone components." +
                                " Reduce number of DynamicBone components for better performance.",
                                perfStats.dynamicBone?.componentCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).dynamicBone.componentCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).dynamicBone.componentCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.DynamicBoneSimulatedBoneCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Dynamic Bone Simulated Bone Count: {0}", perfStats.dynamicBone?.transformCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Dynamic Bone Simulated Bone Count: {0} (Recommended: {1}) - " +
                                "Reduce number of transforms in hierarchy under DynamicBone components, or set EndLength or EndOffset to zero to reduce the number of simulated bones.",
                                perfStats.dynamicBone?.transformCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).dynamicBone.transformCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Dynamic Bone Simulated Bone Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many bones simulated by DynamicBone." +
                                " Reduce number of transforms in hierarchy under DynamicBone components, or set EndLength or EndOffset to zero to reduce the number of simulated bones.",
                                perfStats.dynamicBone?.transformCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).dynamicBone.transformCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).dynamicBone.transformCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.DynamicBoneColliderCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Dynamic Bone Collider Count: {0}", perfStats.dynamicBone?.colliderCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Dynamic Bone Collider Count: {0} (Recommended: {1}) - Avoid use of DynamicBoneColliders for better performance.",
                                perfStats.dynamicBone?.colliderCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).dynamicBone.colliderCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Dynamic Bone Collider Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many DynamicBoneColliders." +
                                " Avoid use of DynamicBoneColliders for better performance.",
                                perfStats.dynamicBone?.colliderCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).dynamicBone.colliderCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).dynamicBone.colliderCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.DynamicBoneCollisionCheckCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Dynamic Bone Collision Check Count: {0}", perfStats.dynamicBone?.collisionCheckCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Dynamic Bone Collision Check Count: {0} (Recommended: {1}) - Avoid use of DynamicBoneColliders for better performance.",
                                perfStats.dynamicBone?.collisionCheckCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).dynamicBone.collisionCheckCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Dynamic Bone Collision Check Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many DynamicBoneColliders." +
                                " Avoid use of DynamicBoneColliders for better performance.",
                                perfStats.dynamicBone?.collisionCheckCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).dynamicBone.collisionCheckCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).dynamicBone.collisionCheckCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.PhysBoneComponentCount:
                {
                    //Max limits
                    if(perfStats.physBone?.componentCount > AvatarValidation.MAX_AVD_PHYSBONES_PER_AVATAR)
                    {
                        displayLevel = PerformanceInfoDisplayLevel.Error;
                        text = $"Phys Bone Components: {perfStats.physBone?.componentCount} - Avatar exceeds the maximum limit ({AvatarValidation.MAX_AVD_PHYSBONES_PER_AVATAR}) of this component type.  Reduce the number of VRCPhysBone components on this avatar.";
                        break;
                    }

                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Phys Bone Components: {0}", perfStats.physBone?.componentCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Phys Bone Components: {0} (Recommended: {1}) - Reduce number of VRCPhysBone components for better performance.",
                                perfStats.physBone?.componentCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).physBone.componentCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = isMobilePlatform ? PerformanceInfoDisplayLevel.Error : PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Phys Bone Components: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many VRCPhysBone components." +
                                " {3}",
                                perfStats.physBone?.componentCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).physBone.componentCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).physBone.componentCount,
                                (isMobilePlatform) ? "All PhysBone components will be removed at runtime." : "Reduce number of VRCPhysBone components for better performance.");

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.PhysBoneTransformCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Phys Bone Transform Count: {0}", perfStats.physBone?.transformCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Phys Bone Transform Count: {0} (Recommended: {1}) - This avatar has many VRCPhysBone transforms and may perform poorly." +
                                "Reduce number of transforms in hierarchy under VRCPhysBone components.",
                                perfStats.physBone?.transformCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).physBone.transformCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = isMobilePlatform ? PerformanceInfoDisplayLevel.Error : PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Phys Bone Transform Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many VRCPhysBone transforms and will perform poorly." +
                                " {3}",
                                perfStats.physBone?.transformCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).physBone.transformCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).physBone.transformCount,
                                (isMobilePlatform) ? "All PhysBone components will be removed at runtime." : "Reduce the number of affected transforms by adding exclusions or removing components.");

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.PhysBoneColliderCount:
                {
                    //Max limits
                    if(perfStats.physBone?.colliderCount > AvatarValidation.MAX_AVD_COLLIDERS_PER_AVATAR)
                    {
                        displayLevel = PerformanceInfoDisplayLevel.Error;
                        text = $"Phys Bone Colliders: {perfStats.physBone?.colliderCount} - Avatar exceeds the maximum limit ({AvatarValidation.MAX_AVD_COLLIDERS_PER_AVATAR}) of this component type.  Reduce the number of VRCPhysBoneCollider components on this avatar.";
                        break;
                    }

                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Phys Bone Collider Count: {0}", perfStats.physBone?.colliderCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Phys Bone Collider Count: {0} (Recommended: {1}) - Reduce the usage of VRCPhysBoneColliders for better performance.",
                                perfStats.physBone?.colliderCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).physBone.colliderCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = isMobilePlatform ? PerformanceInfoDisplayLevel.Error : PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Phys Bone Collider Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many VRCPhysBoneColliders." +
                                " {3}",
                                perfStats.dynamicBone?.colliderCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).dynamicBone.colliderCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).dynamicBone.colliderCount,
                                (isMobilePlatform) ? "All PhysBone colliders will be removed at runtime." : "Reduce number of VRCPhysBone colliders for better performance.");

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.PhysBoneCollisionCheckCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Phys Bone Collision Check Count: {0}", perfStats.physBone?.collisionCheckCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Phys Bone Collision Check Count: {0} (Recommended: {1}) - Reduce the usage of VRCPhysBoneColliders for better performance.",
                                perfStats.physBone?.collisionCheckCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).physBone.collisionCheckCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = isMobilePlatform ? PerformanceInfoDisplayLevel.Error : PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Phys Bone Collision Check Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many VRCPhysBoneColliders." +
                                " {3}",
                                perfStats.physBone?.collisionCheckCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).physBone.collisionCheckCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).physBone.collisionCheckCount,
                                (isMobilePlatform) ? "All PhysBone colliders will be removed at runtime." : "Reduce number of VRCPhysBone colliders for better performance.");

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.ContactCount:
                {
                    //Max limits
                    if(perfStats.contactCount > AvatarValidation.MAX_AVD_CONTACTS_PER_AVATAR)
                    {
                        displayLevel = PerformanceInfoDisplayLevel.Error;
                        text = $"Contact Component Count: {perfStats.contactCount} - Avatar exceeds the maximum limit ({AvatarValidation.MAX_AVD_CONTACTS_PER_AVATAR}) of this component type.  Reduce the number VRCContact components on this avatar.";
                        break;
                    }

                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Contact Component Count: {0}", perfStats.contactCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Contact Component Count: {0} (Recommended: {1}) - Reduce the usage of VRCContact components for optimal performance.",
                                perfStats.contactCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).contactCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = isMobilePlatform ? PerformanceInfoDisplayLevel.Error : PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Contact Component Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many VRCContact components. {3}",
                                perfStats.contactCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).contactCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).contactCount,
                                (isMobilePlatform) ? "All VRCContact components will be removed at runtime." : "Reduce number of VRCContact components for optimal performance.");

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.ClothCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Cloth Component Count: {0}", perfStats.clothCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Cloth Component Count: {0} (Recommended: {1}) - Avoid use of cloth for optimal performance.",
                                perfStats.clothCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).clothCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Cloth Component Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many Cloth components. Avoid use of cloth for optimal performance.",
                                perfStats.clothCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).clothCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).clothCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.ClothMaxVertices:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Cloth Total Vertex Count: {0}", perfStats.clothMaxVertices);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Cloth Total Vertex Count: {0} (Recommended: {1}) - Reduce number of vertices in cloth meshes for improved performance.",
                                perfStats.clothMaxVertices,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).clothMaxVertices);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Cloth Total Vertex Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many vertices in cloth meshes." +
                                " Reduce number of vertices in cloth meshes for improved performance.",
                                perfStats.clothMaxVertices,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).clothMaxVertices,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).clothMaxVertices);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.PhysicsColliderCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Physics Collider Count: {0}", perfStats.physicsColliderCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Physics Collider Count: {0} (Recommended: {1}) - Avoid use of colliders for optimal performance.",
                                perfStats.physicsColliderCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).physicsColliderCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Physics Collider Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many colliders. Avoid use of colliders for optimal performance.",
                                perfStats.physicsColliderCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).physicsColliderCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).physicsColliderCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.PhysicsRigidbodyCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Physics Rigidbody Count: {0}", perfStats.physicsRigidbodyCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Physics Rigidbody Count: {0} (Recommended: {1}) - Avoid use of rigidbodies for optimal performance.",
                                perfStats.physicsRigidbodyCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).physicsRigidbodyCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Physics Rigidbody Count: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many rigidbodies. Avoid use of rigidbodies for optimal performance.",
                                perfStats.physicsRigidbodyCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).physicsRigidbodyCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).physicsRigidbodyCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.AudioSourceCount:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Audio Sources: {0}", perfStats.audioSourceCount);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Audio Sources: {0} (Recommended: {1}) - Reduce number of audio sources for better performance.",
                                perfStats.audioSourceCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).audioSourceCount);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Audio Sources: {0} (Maximum: {1}, Recommended: {2}) - This avatar has too many audio sources. Reduce number of audio sources for better performance.",
                                perfStats.audioSourceCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).audioSourceCount,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).audioSourceCount);

                            break;
                        }
                    }

                    break;
                }
                case AvatarPerformanceCategory.TextureMegabytes:
                {
                    switch(rating)
                    {
                        case PerformanceRating.Excellent:
                        case PerformanceRating.Good:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Verbose;
                            text = string.Format("Texture Memory Usage: {0} MB", perfStats.textureMegabytes);
                            break;
                        }
                        case PerformanceRating.Medium:
                        case PerformanceRating.Poor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Texture Memory Usage: {0} MB (Recommended: {1} MB) - Lower the resolution of your textures in the texture import settings.",
                                perfStats.textureMegabytes,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).textureMegabytes);

                            break;
                        }
                        case PerformanceRating.VeryPoor:
                        {
                            displayLevel = PerformanceInfoDisplayLevel.Warning;
                            text = string.Format(
                                "Texture Memory Usage: {0} MB (Maximum: {1} MB, Recommended: {2} MB) - This avatar's total texture resolution is too high. " +
                                "Lower the resolution of your largest textures in the texture import settings. " +
                                (
                                    ValidationEditorHelpers.IsMobilePlatform()
                                    ? ""
                                    : "Consider using shader features like decals or tiling. Check your shader's documentation for more information. "
                                ) +
                                "You can also check your model's UV layouts to better utilize texture space. These techniques may help you reduce texture memory usage.",
                                perfStats.textureMegabytes,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Poor, isMobilePlatform).textureMegabytes,
                                AvatarPerformanceStats.GetStatLevelForRating(PerformanceRating.Excellent, isMobilePlatform).textureMegabytes);

                            break;
                        }
                    }
                    break;
                }
                default:
                {
                    text = "";
                    displayLevel = PerformanceInfoDisplayLevel.None;
                    break;
                }
            }
        }
    }
}
#endif
