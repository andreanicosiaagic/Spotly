import base64
import json
import os
from datetime import date
from pathlib import Path
from tempfile import gettempdir

from playwright.sync_api import sync_playwright


def build_dev_token(profile: dict[str, object]) -> str:
    payload = json.dumps(profile, separators=(",", ":")).encode("utf-8")
    encoded = base64.urlsafe_b64encode(payload).decode("utf-8").rstrip("=")
    return f"dev.{encoded}"


MANAGER_TOKEN = build_dev_token(
    {
        "sub": "u2",
        "name": "Marco Bianchi",
        "role": "Manager",
        "department": "Engineering",
        "eligibility": [],
    }
)

FACILITY_TOKEN = build_dev_token(
    {
        "sub": "u3",
        "name": "Sara Conti",
        "role": "Facility",
        "department": "Facility",
        "eligibility": ["guest", "ev"],
    }
)

BASE_URL = os.environ.get("SPOTLY_E2E_BASE_URL", "http://127.0.0.1:5173")


with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page(viewport={"width": 1440, "height": 980})
    page.goto(f"{BASE_URL}/lunch")
    page.wait_for_load_state("load")

    page.get_by_role("heading", name="Pranzo", exact=True).wait_for()
    profile_select = page.locator("label:has-text('Profilo demo') select:visible").first
    profile_select.select_option("manager")
    active_date = page.locator(".date-chip-active:visible").first.get_attribute("data-date")
    assert active_date, "La data attiva deve essere esposta dal date strip"

    manager_tick = page.context.request.post(
        f"{BASE_URL}/api/lunch/demo/tick?date={active_date}",
        headers={"Authorization": f"Bearer {MANAGER_TOKEN}"},
    )
    assert manager_tick.status == 403

    profile_select.select_option("facility")
    page.get_by_role("button", name="Aggiornamento demo").wait_for()
    page.get_by_text("Realtime attivo").wait_for(timeout=15_000)

    bistrot_card = page.locator("button:visible").filter(has_text="Bistrot Verde").first
    initial_seats = int(bistrot_card.locator("strong").inner_text().split()[0])

    facility_tick = page.context.request.post(
        f"{BASE_URL}/api/lunch/demo/tick?date={active_date}",
        headers={"Authorization": f"Bearer {FACILITY_TOKEN}"},
    )
    assert facility_tick.ok

    page.wait_for_function(
        """
        initial => {
          const card = [...document.querySelectorAll('button')].find((el) => el.textContent?.includes('Bistrot Verde'));
          const text = card?.querySelector('strong')?.textContent ?? '';
          return Number(text.split(' ')[0]) !== initial;
        }
        """,
        arg=initial_seats,
    )
    page.locator("section[aria-labelledby='message-feed'] li").first.wait_for()

    bistrot_card.click()
    detail = page.locator("section").filter(has_text="Dettaglio prenotazione").first
    detail.wait_for()
    detail.get_by_role("button").filter(has_text="12:00").first.click()
    detail.locator("label").filter(has_text="Pasta al pomodoro").first.click()
    detail.get_by_role("button", name="Conferma prenotazione").click()

    confirmation = page.get_by_role("status")
    confirmation.get_by_text("Prenotazione confermata").wait_for(timeout=15_000)
    active_booking = page.locator("section").filter(has_text="Prenotazione attiva").first
    active_booking.wait_for()
    assert str(date.today().year) in active_booking.inner_text()

    page.reload()
    page.wait_for_load_state("load")
    page.locator("section").filter(has_text="Prenotazione attiva").first.wait_for()

    page.goto(f"{BASE_URL}/")
    page.wait_for_load_state("load")
    lunch_card = page.locator(".app-content a[href='/lunch']").first
    lunch_card.wait_for()
    lunch_card.get_by_text("Ristorante").wait_for()
    assert "Ristorante" in lunch_card.inner_text()

    page.goto(f"{BASE_URL}/lunch")
    page.wait_for_load_state("load")
    page.locator("section").filter(has_text="Prenotazione attiva").first.get_by_role("button", name="Annulla").click()
    page.wait_for_function(
        "() => ![...document.querySelectorAll('section')].some((section) => section.textContent?.includes('Prenotazione attiva'))"
    )

    page.screenshot(path=str(Path(gettempdir()) / "spotly-integrated-smoke.png"), full_page=True)
    browser.close()
