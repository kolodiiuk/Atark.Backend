import random
from locust import HttpUser, between, task

class SpotRentUser(HttpUser):
    wait_time = between(0.5, 2)

    def on_start(self):
        self.space_ids = self._load_space_ids()

    @task(4)
    def get_space(self):
        if not self.space_ids:
            return
        space_id = random.choice(self.space_ids)
        self.client.get(f"/api/spaces/{space_id}", name="GET /api/spaces/{id}")

    @task(3)
    def filter_spaces(self):
        # Matches SpacesController query params and keeps ranges valid.
        params = {
            "limit": random.choice([10, 20, 30]),
            "offset": random.choice([0, 10, 20]),
            "minCapacity": random.choice([1, 2, 4]),
            "maxCapacity": random.choice([8, 12, 20]),
            "sort": random.choice(["price_asc", "price_desc", "newest"]),
        }
        self.client.get("/api/spaces", params=params, name="GET /api/spaces (filter)")

    @task(2)
    def get_subscription_plans(self):
        self.client.get(
            "/api/subscription-plans",
            name="GET /api/subscription-plans",
        )

    def _load_space_ids(self):
        resp = self.client.get("/api/spaces?limit=20&offset=0", name="GET /api/spaces")
        if resp.status_code != 200:
            return []
        try:
            body = resp.json()
            items = body.get("data") or body.get("Data") or []
            ids = [item.get("id") or item.get("Id") for item in items if isinstance(item, dict)]
            return [space_id for space_id in ids if isinstance(space_id, int) and space_id > 0]
        except ValueError:
            return []
