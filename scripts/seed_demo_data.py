"""
Carga datos de demostracion en la API de SmartGas: una cuenta por segmento (hogar y restaurante)
con zonas, sensores, lecturas (que generan incidentes y alertas) y contacto de emergencia.

Uso:
    python scripts/seed_demo_data.py https://backend-rxmh.onrender.com/api/v1

Se puede ejecutar mas de una vez: si la cuenta, la zona o el sensor ya existen, los reutiliza.
Solo usa la libreria estandar de Python.
"""
import json
import sys
import urllib.error
import urllib.request

PASSWORD = "SmartGas2026"

ACCOUNTS = [
    {
        "signUp": {
            "fullName": "Lucía Paredes Rojas",
            "email": "lucia.paredes@smartgas.demo",
            "role": "HomeOwner",
            "phone": "987 412 305",
            "district": "Surco",
        },
        "planId": 1,
        "contact": {"name": "Jorge Paredes", "phone": "956 778 120", "email": "jorge.paredes@smartgas.demo"},
        "zones": [
            {
                "name": "Cocina familiar",
                "description": "Cocina de 4 hornillas con balón de 10 kg bajo el mostrador",
                "sensitivity": "High",
                "sensors": [
                    {"code": "HOG-CK-01", "name": "Sensor bajo el mostrador", "type": "Gas", "locationDetail": "Junto al balón de 10 kg, a 20 cm del piso"},
                    {"code": "HOG-CK-02", "name": "Sensor sobre la cocina", "type": "MultiSensor", "locationDetail": "Campana extractora, lado izquierdo"},
                ],
            },
            {
                "name": "Lavandería con terma",
                "description": "Patio techado donde está la terma a gas de 26 litros",
                "sensitivity": "Medium",
                "sensors": [
                    {"code": "HOG-LV-03", "name": "Sensor de la terma", "type": "MultiSensor", "locationDetail": "Pared detrás de la terma, a 1.5 m"},
                ],
            },
        ],
        "readings": [
            ("HOG-CK-01", 12, None), ("HOG-CK-02", 9, 31), ("HOG-LV-03", 6, 27),
            ("HOG-CK-01", 64, None), ("HOG-CK-02", 18, 49),
        ],
    },
    {
        "signUp": {
            "fullName": "Carlos Quispe Mendoza",
            "email": "carlos.quispe@smartgas.demo",
            "role": "RestaurantAdministrator",
            "businessName": "Cevichería El Muelle",
            "phone": "944 210 887",
            "district": "Chorrillos",
        },
        "planId": 2,
        "contact": {"name": "Rosa Mendoza", "phone": "912 345 678", "email": "rosa.mendoza@smartgas.demo"},
        "zones": [
            {
                "name": "Cocina caliente",
                "description": "Línea de 3 cocinas industriales para arroces y saltados",
                "sensitivity": "Medium",
                "sensors": [
                    {"code": "MUE-CC-01", "name": "Sensor de la línea caliente", "type": "MultiSensor", "locationDetail": "Detrás de la cocina industrial central"},
                    {"code": "MUE-CC-02", "name": "Sensor de la campana", "type": "Temperature", "locationDetail": "Dentro de la campana, junto al filtro"},
                ],
            },
            {
                "name": "Zona de freidoras",
                "description": "Dos freidoras a gas para chicharrón de pescado y yuca",
                "sensitivity": "High",
                "sensors": [
                    {"code": "MUE-FR-03", "name": "Sensor entre freidoras", "type": "MultiSensor", "locationDetail": "Entre la freidora 1 y la freidora 2"},
                ],
            },
            {
                "name": "Almacén de balones",
                "description": "Depósito ventilado con 4 balones de 45 kg conectados al manifold",
                "sensitivity": "High",
                "sensors": [
                    {"code": "MUE-AL-04", "name": "Sensor del manifold", "type": "Gas", "locationDetail": "Al lado del manifold, a 30 cm del piso"},
                    {"code": "MUE-AL-05", "name": "Sensor de la puerta", "type": "Gas", "locationDetail": "Marco de la puerta del almacén"},
                ],
            },
            {
                "name": "Parrilla de mariscos",
                "description": "Parrilla a gas en la terraza para pulpo y conchas a la parrilla",
                "sensitivity": "Low",
                "sensors": [
                    {"code": "MUE-PA-06", "name": "Sensor de la terraza", "type": "MultiSensor", "locationDetail": "Columna junto a la parrilla"},
                ],
            },
        ],
        "readings": [
            ("MUE-CC-01", 22, 38), ("MUE-CC-02", None, 44), ("MUE-FR-03", 15, 52),
            ("MUE-AL-04", 8, None), ("MUE-AL-05", 81, None), ("MUE-PA-06", 11, 35),
            ("MUE-CC-01", 27, 41),
        ],
    },
]


def call(base, method, path, body=None):
    request = urllib.request.Request(
        f"{base}/{path}",
        data=None if body is None else json.dumps(body).encode(),
        method=method,
        headers={"Content-Type": "application/json", "Accept": "application/json"},
    )
    try:
        with urllib.request.urlopen(request, timeout=90) as response:
            raw = response.read()
            return response.status, json.loads(raw) if raw else None
    except urllib.error.HTTPError as error:
        return error.code, None


def main():
    base = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else "http://localhost:5048/api/v1"
    total = 0

    for data in ACCOUNTS:
        sign_up = dict(data["signUp"], password=PASSWORD)
        status, account = call(base, "POST", "auth/sign-up", sign_up)
        if status == 409:
            status, account = call(base, "POST", "auth/sign-in", {"email": sign_up["email"], "password": PASSWORD})
        if status not in (200, 201):
            sys.exit(f"No se pudo crear ni iniciar sesión con {sign_up['email']} (HTTP {status})")
        account_id = account["accountId"]
        total += 1
        print(f"Cuenta {sign_up['email']} (id {account_id})")

        call(base, "PATCH", f"subscriptions/current/{account_id}/change-plan", {"planId": data["planId"]})
        call(base, "PATCH", f"emergency-contacts/{account_id}", data["contact"])
        total += 1

        _, existing_zones = call(base, "GET", f"zones?accountId={account_id}")
        _, existing_sensors = call(base, "GET", f"sensors?accountId={account_id}")
        zones_by_name = {z["name"]: z for z in existing_zones or []}
        sensor_codes = {s["code"] for s in existing_sensors or []}

        for zone in data["zones"]:
            if zone["name"] not in zones_by_name:
                status, created = call(base, "POST", "zones", {
                    "accountId": account_id,
                    "name": zone["name"],
                    "description": zone["description"],
                    "sensitivity": zone["sensitivity"],
                })
                if status not in (200, 201):
                    sys.exit(f"No se pudo crear la zona {zone['name']} (HTTP {status})")
                zones_by_name[zone["name"]] = created
                total += 1
            zone_id = zones_by_name[zone["name"]]["id"]
            print(f"  Zona {zone['name']} (id {zone_id})")

            for sensor in zone["sensors"]:
                if sensor["code"] in sensor_codes:
                    continue
                status, _ = call(base, "POST", "sensors", dict(sensor, accountId=account_id, zoneId=zone_id))
                if status not in (200, 201):
                    sys.exit(f"No se pudo crear el sensor {sensor['code']} (HTTP {status})")
                sensor_codes.add(sensor["code"])
                total += 1
                print(f"    Sensor {sensor['code']}")

        for code, gas, temperature in data["readings"]:
            status, _ = call(base, "POST", "sensor-readings", {"sensorCode": code, "gasLevel": gas, "temperature": temperature})
            if status in (200, 201):
                total += 1
        print(f"  {len(data['readings'])} lecturas enviadas")

    print(f"Listo: {total} registros creados o actualizados.")


if __name__ == "__main__":
    main()
