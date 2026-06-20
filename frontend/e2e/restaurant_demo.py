from pathlib import Path
from tempfile import gettempdir

from playwright.sync_api import sync_playwright


with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page(viewport={"width": 430, "height": 932})
    page.goto("http://127.0.0.1:5173/lunch")
    # This page intentionally polls every three seconds in demo mode, so networkidle is unreachable.
    page.wait_for_load_state("load")

    page.get_by_role("heading", name="Pranzo", exact=True).wait_for()
    bistrot = page.get_by_role("button").filter(has_text="Bistrot Verde")
    initial_seats = int(bistrot.locator("strong").inner_text().split()[0])

    page.get_by_role("button", name="Aggiornamento demo").click()
    page.wait_for_function(
        "initial => { const card = [...document.querySelectorAll('button')].find(el => el.textContent.includes('Bistrot Verde')); return Number(card?.querySelector('strong')?.textContent.split(' ')[0]) !== initial; }",
        arg=initial_seats,
    )
    updated_seats = int(bistrot.locator("strong").inner_text().split()[0])
    assert updated_seats != initial_seats
    page.get_by_text("posti aggiornati", exact=False).first.wait_for()

    bistrot.click()
    confirmation = page.get_by_role("status")
    confirmation.get_by_text("Prenotazione confermata").wait_for()
    assert "Restano" in confirmation.inner_text()
    assert int(bistrot.locator("strong").inner_text().split()[0]) == updated_seats - 1

    page.screenshot(path=str(Path(gettempdir()) / "spotly-restaurant-demo.png"), full_page=True)
    browser.close()
