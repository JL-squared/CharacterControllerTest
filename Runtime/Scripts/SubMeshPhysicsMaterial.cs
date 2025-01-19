using System;
using System.Collections.Generic;
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

    private int GetMaterialIndex(uint triangleOffset) {
        for (int j = offsets.Length - 1; j >= 0; j--) {
            uint offset = offsets[j];
            if (triangleOffset >= offset) {
                return j;
            }
        }

        return -1;
    }

    public PhysicsMaterialTemp GetMaterial(uint triangleOffset) {
        return materials[GetMaterialIndex(triangleOffset)];
    } 

    private void Physics_ContactModifyEvent(PhysicsScene arg1, Unity.Collections.NativeArray<ModifiableContactPair> pairs) {
        foreach (var pair in pairs) {
            if (pair.colliderInstanceID == instanceId || pair.otherColliderInstanceID == instanceId) {
                for (int i = 0; i < pair.contactCount; i++) {
                    uint face = pair.GetFaceIndex(i);
                    int material = GetMaterialIndex(face * 3);


                    pair.SetDynamicFriction(i, materials[material].dynamicFriction);
                    pair.SetStaticFriction(i, materials[material].staticFriction);
                    pair.SetBounciness(i, materials[material].bounce);
                }
            }
        }
    }
}
