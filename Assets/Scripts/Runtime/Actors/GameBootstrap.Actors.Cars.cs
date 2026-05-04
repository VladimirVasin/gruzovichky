using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private string GenerateWorkerName(WorkerGender gender)
    {
        string[] pool = gender == WorkerGender.Female ? WorkerFemaleFirstNames : WorkerMaleFirstNames;
        string first = pool[Random.Range(0, pool.Length)];
        string last  = WorkerLastNames[Random.Range(0, WorkerLastNames.Length)];
        return $"{first} {last}";
    }

    private GameObject CreateCarModel(int modelIndex, Transform parent)
    {
        int idx = Mathf.Clamp(modelIndex, 0, CarModelNames.Length - 1);
        GameObject root = new($"Car_{CarModelNames[idx]}");
        root.transform.SetParent(parent, false);

        switch (idx)
        {
            case 1:
                BuildPickupCar(root.transform, CarBodyColors[idx]);
                break;
            case 2:
                BuildHatchbackCar(root.transform, CarBodyColors[idx]);
                break;
            default:
                BuildSedanCar(root.transform, CarBodyColors[idx]);
                break;
        }

        return root;
    }

    private void BuildSedanCar(Transform parent, Color bodyColor)
    {
        CreateCarCube(parent, "SedanBody", new Vector3(0f, 0.14f, 0f), new Vector3(0.92f, 0.22f, 0.44f), bodyColor);
        CreateCarCube(parent, "SedanRoof", new Vector3(-0.08f, 0.30f, 0f), new Vector3(0.50f, 0.16f, 0.40f), Color.Lerp(bodyColor, Color.white, 0.12f), VisualSmoothnessRoofMetal);
        CreateCarCube(parent, "SedanWindshield", new Vector3(0.20f, 0.31f, 0f), new Vector3(0.06f, 0.12f, 0.36f), new Color(0.65f, 0.84f, 0.92f), VisualSmoothnessGlass);
        CreateCarCube(parent, "SedanRearGlass", new Vector3(-0.36f, 0.30f, 0f), new Vector3(0.05f, 0.12f, 0.34f), new Color(0.58f, 0.78f, 0.88f), VisualSmoothnessGlass);
        CreateCarWheels(parent, 0.34f, 0.25f, 0.095f, 0.08f);
    }

    private void BuildPickupCar(Transform parent, Color bodyColor)
    {
        CreateCarCube(parent, "PickupBody", new Vector3(0f, 0.17f, 0f), new Vector3(0.88f, 0.30f, 0.48f), bodyColor);
        CreateCarCube(parent, "PickupCab", new Vector3(0.22f, 0.40f, 0f), new Vector3(0.42f, 0.24f, 0.44f), Color.Lerp(bodyColor, Color.white, 0.10f), VisualSmoothnessVehicleMetal);
        CreateCarCube(parent, "PickupBedLeft", new Vector3(-0.24f, 0.38f, -0.24f), new Vector3(0.46f, 0.12f, 0.05f), Color.Lerp(bodyColor, Color.black, 0.12f));
        CreateCarCube(parent, "PickupBedRight", new Vector3(-0.24f, 0.38f, 0.24f), new Vector3(0.46f, 0.12f, 0.05f), Color.Lerp(bodyColor, Color.black, 0.12f));
        CreateCarCube(parent, "PickupBedGate", new Vector3(-0.48f, 0.38f, 0f), new Vector3(0.05f, 0.12f, 0.46f), Color.Lerp(bodyColor, Color.black, 0.12f));
        CreateCarCube(parent, "PickupWindshield", new Vector3(0.45f, 0.41f, 0f), new Vector3(0.05f, 0.13f, 0.34f), new Color(0.65f, 0.84f, 0.92f), VisualSmoothnessGlass);
        CreateCarWheels(parent, 0.35f, 0.28f, 0.115f, 0.10f);
    }

    private void BuildHatchbackCar(Transform parent, Color bodyColor)
    {
        CreateCarCube(parent, "HatchBody", new Vector3(0f, 0.15f, 0f), new Vector3(0.74f, 0.26f, 0.42f), bodyColor);
        CreateCarCube(parent, "HatchRoof", new Vector3(0.02f, 0.28f, 0f), new Vector3(0.68f, 0.18f, 0.40f), Color.Lerp(bodyColor, Color.white, 0.10f), VisualSmoothnessRoofMetal);
        CreateCarCube(parent, "HatchWindshield", new Vector3(0.38f, 0.28f, 0f), new Vector3(0.05f, 0.12f, 0.34f), new Color(0.65f, 0.84f, 0.92f), VisualSmoothnessGlass);
        CreateCarCube(parent, "HatchRearGlass", new Vector3(-0.34f, 0.27f, 0f), new Vector3(0.05f, 0.13f, 0.34f), new Color(0.58f, 0.78f, 0.88f), VisualSmoothnessGlass);
        CreateCarWheels(parent, 0.28f, 0.24f, 0.09f, 0.07f);
    }

    private GameObject CreateCarCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, float smoothness = VisualSmoothnessVehicleMetal)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        ApplyColor(part, color, smoothness);
        ConfigureShadowVisual(part, smoothness);
        DisableCarCollider(part);
        return part;
    }

    private GameObject CreateCarWheel(Transform parent, string name, Vector3 localPosition, float radius, float width)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = name;
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = localPosition;
        wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        wheel.transform.localScale = new Vector3(radius, width * 0.5f, radius);
        ApplyColor(wheel, new Color(0.14f, 0.14f, 0.14f), VisualSmoothnessRubber);
        ConfigureShadowVisual(wheel, VisualSmoothnessRubber);
        DisableCarCollider(wheel);
        return wheel;
    }

    private void CreateCarWheels(Transform parent, float xOffset, float zOffset, float radius, float width)
    {
        CreateCarWheel(parent, "WheelFrontLeft", new Vector3(xOffset, 0.08f, -zOffset), radius, width);
        CreateCarWheel(parent, "WheelFrontRight", new Vector3(xOffset, 0.08f, zOffset), radius, width);
        CreateCarWheel(parent, "WheelRearLeft", new Vector3(-xOffset, 0.08f, -zOffset), radius, width);
        CreateCarWheel(parent, "WheelRearRight", new Vector3(-xOffset, 0.08f, zOffset), radius, width);
    }

    private static void DisableCarCollider(GameObject part)
    {
        if (part != null && part.TryGetComponent(out Collider collider))
        {
            collider.enabled = false;
        }
    }

    private void SpawnWorkerCarAtParking(DriverAgent driver)
    {
        if (driver == null || driver.OwnedCarModelIndex < 0 || !locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            return;
        }

        if (driver.OwnedCarObject != null)
        {
            Object.Destroy(driver.OwnedCarObject);
            driver.OwnedCarObject = null;
        }

        int slotIndex = Mathf.Max(0, driverAgents.IndexOf(driver));
        Vector3 center = GetLocationCenter(parking);
        Vector3 pos = new(
            center.x + ((slotIndex % 4) - 1.5f) * 1.3f,
            0f,
            center.z - 3f - (slotIndex / 4) * 1.6f);
        pos = WithRoadVehicleHeight(pos, LocalBusRoadSurfaceLift);

        GameObject car = CreateCarModel(driver.OwnedCarModelIndex, worldRoot);
        car.transform.position = pos;
        car.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        car.transform.localScale = Vector3.one;
        driver.OwnedCarObject = car;
    }


}
