using UnityEngine;
using System.Collections;

namespace NWO
{
    /// <summary>
    /// Pre-warms all Animator controllers in the scene so animation clips
    /// and their sprite textures are uploaded to the GPU during load
    /// instead of on first play (eliminating first-frame stutter).
    /// Attach to any persistent GameObject or let GameManager auto-create.
    /// </summary>
    public sealed class AnimationPreloader : MonoBehaviour
    {
        private static bool _preloaded;

        private void Start()
        {
            if (!_preloaded)
            {
                _preloaded = true;
                StartCoroutine(PreloadAllAnimators());
            }
        }

        private IEnumerator PreloadAllAnimators()
        {
            // Find all animators in scene (including inactive)
            var animators = FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var animator in animators)
            {
                if (animator == null || animator.runtimeAnimatorController == null)
                    continue;

                // Force the animator to initialize and upload all clip textures
                // by sampling each clip at frame 0
                var clips = animator.runtimeAnimatorController.animationClips;
                foreach (var clip in clips)
                {
                    if (clip != null)
                    {
                        clip.SampleAnimation(animator.gameObject, 0f);
                    }
                }
            }

            // Yield one frame to let GPU process the uploaded textures
            yield return null;

            // Reset all animators to their default state after preloading
            foreach (var animator in animators)
            {
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.Rebind();
                    animator.Update(0f);
                }
            }
        }

        /// <summary>
        /// Call to allow preloading again on scene reload.
        /// </summary>
        public static void ResetPreloadFlag()
        {
            _preloaded = false;
        }
    }
}
