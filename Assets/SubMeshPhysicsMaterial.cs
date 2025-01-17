using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[Serializable]
public struct PhysicsMaterialTemp {
    public float dynamicFriction;
    public float staticFriction;
    public float bounce;
}

public class SubMeshPhysicsMaterial : MonoBehaviour {
    public MeshCollider meshCollider;
    public List<PhysicsMaterialTemp> materials;
    private uint[] offsets;
    private int instanceId;

    private void Start() {
        meshCollider.hasModifiableContacts = true;
        Physics.ContactModifyEvent += Physics_ContactModifyEvent;
        instanceId = meshCollider.GetInstanceID();

        Mesh mesh = meshCollider.sharedMesh;
        if (mesh.subMeshCount != materials.Count) {
            throw new Exception("uh oh");
        }

        offsets = new uint[materials.Count];
        for (int i = 0; i < offsets.Length; i++) {
            offsets[i] = (uint)mesh.GetSubMesh(i).indexStart;
        }
    }

    public void OnDisable() {
        Physics.ContactModifyEvent -= Physics_ContactModifyEvent;
    }

    private void Physics_ContactModifyEvent(PhysicsScene arg1, Unity.Collections.NativeArray<ModifiableContactPair> pairs) {
        foreach (var pair in pairs) {
            if (pair.colliderInstanceID == instanceId || pair.otherColliderInstanceID == instanceId) {
                for (int i = 0; i < pair.contactCount; i++) {
                    uint face = pair.GetFaceIndex(i);

                    int material = 0;
                    for (int j = offsets.Length - 1; j >= 0; j--) {
                        uint offset = offsets[j];
                        if (face*3 >= offset) {
                            material = j;
                            break;
                        }
                    }

                    pair.SetDynamicFriction(i, materials[material].dynamicFriction);
                    pair.SetStaticFriction(i, materials[material].staticFriction);
                    pair.SetBounciness(i, materials[material].bounce);
                }
            }
        }
    }
}
