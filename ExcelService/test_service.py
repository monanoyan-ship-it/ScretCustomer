"""
Excel Service Test Script
Bu script ile servisi lokal olarak test edebilirsiniz.
"""
import requests
import json
import os

BASE_URL = "http://localhost:8002"


def test_health():
    """Health check testi"""
    print("\n=== Health Check Testi ===")
    response = requests.get(f"{BASE_URL}/health")
    print(f"Status: {response.status_code}")
    print(f"Response: {response.json()}")
    return response.status_code == 200


def test_generate_template():
    """Template olusturma testi"""
    print("\n=== Template Olusturma Testi ===")

    template = {
        "name": "Kullanici Listesi",
        "description": "Kullanici import sablonu",
        "entity_type": "User",
        "sheet_name": "Kullanicilar",
        "has_header": True,
        "columns": [
            {
                "column_name": "Kullanici Adi",
                "property_name": "username",
                "column_type": "Text",
                "order": 1,
                "is_required": True,
                "description": "Benzersiz kullanici adi"
            },
            {
                "column_name": "E-posta",
                "property_name": "email",
                "column_type": "Email",
                "order": 2,
                "is_required": True,
                "description": "Gecerli email adresi"
            },
            {
                "column_name": "Ad",
                "property_name": "firstName",
                "column_type": "Text",
                "order": 3,
                "is_required": True
            },
            {
                "column_name": "Soyad",
                "property_name": "lastName",
                "column_type": "Text",
                "order": 4,
                "is_required": True
            },
            {
                "column_name": "Rol",
                "property_name": "role",
                "column_type": "Dropdown",
                "order": 5,
                "is_required": True,
                "dropdown_options": ["Admin", "Manager", "User"],
                "description": "Kullanici rolu"
            },
            {
                "column_name": "Aktif",
                "property_name": "isActive",
                "column_type": "Boolean",
                "order": 6,
                "is_required": False,
                "sample_value": "true"
            },
            {
                "column_name": "Telefon",
                "property_name": "phone",
                "column_type": "Phone",
                "order": 7,
                "is_required": False
            },
            {
                "column_name": "Dogum Tarihi",
                "property_name": "birthDate",
                "column_type": "Date",
                "order": 8,
                "is_required": False
            }
        ]
    }

    response = requests.post(
        f"{BASE_URL}/generate-template",
        json=template
    )

    print(f"Status: {response.status_code}")

    if response.status_code == 200:
        # Excel dosyasini kaydet
        output_file = "test_output/kullanici_template.xlsx"
        os.makedirs("test_output", exist_ok=True)

        with open(output_file, "wb") as f:
            f.write(response.content)
        print(f"Excel dosyasi kaydedildi: {output_file}")
        return True
    else:
        print(f"Hata: {response.text}")
        return False


def test_parse_excel():
    """Excel parse testi"""
    print("\n=== Excel Parse Testi ===")

    # Once bir test dosyasi olustur
    test_file = "test_output/kullanici_template.xlsx"

    if not os.path.exists(test_file):
        print("Once test_generate_template() calistirilmali!")
        return False

    template = {
        "name": "Kullanici Listesi",
        "description": "Kullanici import sablonu",
        "entity_type": "User",
        "sheet_name": "Kullanicilar",
        "has_header": True,
        "columns": [
            {
                "column_name": "Kullanici Adi",
                "property_name": "username",
                "column_type": "Text",
                "order": 1,
                "is_required": True
            },
            {
                "column_name": "E-posta",
                "property_name": "email",
                "column_type": "Email",
                "order": 2,
                "is_required": True
            },
            {
                "column_name": "Ad",
                "property_name": "firstName",
                "column_type": "Text",
                "order": 3,
                "is_required": True
            },
            {
                "column_name": "Soyad",
                "property_name": "lastName",
                "column_type": "Text",
                "order": 4,
                "is_required": True
            },
            {
                "column_name": "Rol",
                "property_name": "role",
                "column_type": "Dropdown",
                "order": 5,
                "is_required": True,
                "dropdown_options": ["Admin", "Manager", "User"]
            },
            {
                "column_name": "Aktif",
                "property_name": "isActive",
                "column_type": "Boolean",
                "order": 6,
                "is_required": False
            },
            {
                "column_name": "Telefon",
                "property_name": "phone",
                "column_type": "Phone",
                "order": 7,
                "is_required": False
            },
            {
                "column_name": "Dogum Tarihi",
                "property_name": "birthDate",
                "column_type": "Date",
                "order": 8,
                "is_required": False
            }
        ]
    }

    with open(test_file, "rb") as f:
        files = {"file": ("test.xlsx", f, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")}
        data = {"template": json.dumps(template)}

        response = requests.post(
            f"{BASE_URL}/parse-excel",
            files=files,
            data=data
        )

    print(f"Status: {response.status_code}")

    if response.status_code == 200:
        result = response.json()
        print(f"Toplam satir: {result['total_rows']}")
        print(f"Gecerli satir: {result['valid_rows']}")
        print(f"Gecersiz satir: {result['invalid_rows']}")

        for row in result['rows']:
            print(f"\nSatir {row['row_number']}:")
            print(f"  Data: {row['data']}")
            if row['errors']:
                print(f"  Hatalar: {row['errors']}")
        return True
    else:
        print(f"Hata: {response.text}")
        return False


def test_sube_template():
    """Sube template testi"""
    print("\n=== Sube Template Testi ===")

    template = {
        "name": "Sube Listesi",
        "description": "Sube import sablonu",
        "entity_type": "Branch",
        "sheet_name": "Subeler",
        "has_header": True,
        "columns": [
            {
                "column_name": "Sube Adi",
                "property_name": "name",
                "column_type": "Text",
                "order": 1,
                "is_required": True,
                "sample_value": "Merkez Sube"
            },
            {
                "column_name": "Sube Kodu",
                "property_name": "code",
                "column_type": "Text",
                "order": 2,
                "is_required": True,
                "sample_value": "MRK001"
            },
            {
                "column_name": "Adres",
                "property_name": "address",
                "column_type": "Text",
                "order": 3,
                "is_required": False,
                "sample_value": "Ataturk Cad. No:123"
            },
            {
                "column_name": "Sehir",
                "property_name": "city",
                "column_type": "Text",
                "order": 4,
                "is_required": True,
                "sample_value": "Istanbul"
            },
            {
                "column_name": "Bolge",
                "property_name": "region",
                "column_type": "Dropdown",
                "order": 5,
                "is_required": False,
                "dropdown_options": ["Marmara", "Ege", "Akdeniz", "Ic Anadolu", "Karadeniz", "Dogu Anadolu", "Guneydogu Anadolu"],
                "sample_value": "Marmara"
            },
            {
                "column_name": "Aktif",
                "property_name": "isActive",
                "column_type": "Boolean",
                "order": 6,
                "is_required": False,
                "sample_value": "true"
            }
        ]
    }

    response = requests.post(
        f"{BASE_URL}/generate-template",
        json=template
    )

    print(f"Status: {response.status_code}")

    if response.status_code == 200:
        output_file = "test_output/sube_template.xlsx"
        os.makedirs("test_output", exist_ok=True)

        with open(output_file, "wb") as f:
            f.write(response.content)
        print(f"Excel dosyasi kaydedildi: {output_file}")
        return True
    else:
        print(f"Hata: {response.text}")
        return False


def run_all_tests():
    """Tum testleri calistir"""
    print("=" * 50)
    print("EXCEL SERVICE TEST SUITE")
    print("=" * 50)

    results = {
        "Health Check": test_health(),
        "Template Olusturma (User)": test_generate_template(),
        "Template Olusturma (Branch)": test_sube_template(),
        "Excel Parse": test_parse_excel()
    }

    print("\n" + "=" * 50)
    print("TEST SONUCLARI")
    print("=" * 50)

    for test_name, passed in results.items():
        status = "PASSED" if passed else "FAILED"
        print(f"{test_name}: {status}")

    total = len(results)
    passed = sum(results.values())
    print(f"\nToplam: {passed}/{total} test basarili")


if __name__ == "__main__":
    run_all_tests()
