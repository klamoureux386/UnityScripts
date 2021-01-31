using UnityEngine;

namespace MethodExtensions {

    public static class Extensions {

        //To do: test cases :)
        //Returns the normalized (length 1) direction from A towards B
        public static Vector3 normDirectionTo(this Vector3 A, Vector3 B) {

            return (B - A).normalized;
        }

        public static Vector3 abs(this Vector3 vec) {

            vec.x = Mathf.Abs(vec.x);
            vec.y = Mathf.Abs(vec.y);
            vec.z = Mathf.Abs(vec.z);

            return vec;
        }

    }
}
