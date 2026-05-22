import csv
import json
import math
import sys
from pathlib import Path

import matplotlib.pyplot as plt
from matplotlib.gridspec import GridSpec
from matplotlib.ticker import MaxNLocator


BG = "#101418"
PANEL = "#161c22"
GRID = "#2a343d"
TEXT = "#e8edf2"
MUTED = "#9aa8b5"
PREY = "#7adf91"
PREDATOR = "#ff6b6b"
JUVENILE = "#57c7ff"
ADULT = "#f7c948"
ENERGY_PREY = "#2dd4bf"
ENERGY_PREDATOR = "#f97316"
DEATH = "#ef476f"
BIRTH = "#9b87f5"
GRASS = "#a3e635"
SHELTER = "#60a5fa"


def load_rows(csv_path):
    with open(csv_path, newline="", encoding="utf-8-sig") as handle:
        reader = csv.DictReader(handle)
        rows = []
        for row in reader:
            parsed = {}
            for key, value in row.items():
                if value is None or value == "":
                    parsed[key] = 0.0
                    continue
                try:
                    parsed[key] = float(value)
                except ValueError:
                    parsed[key] = value
            rows.append(parsed)
        return rows


def col(rows, name):
    return [row.get(name, 0.0) for row in rows]


def style():
    plt.rcParams.update(
        {
            "figure.facecolor": BG,
            "axes.facecolor": PANEL,
            "axes.edgecolor": GRID,
            "axes.labelcolor": MUTED,
            "axes.titlecolor": TEXT,
            "xtick.color": MUTED,
            "ytick.color": MUTED,
            "grid.color": GRID,
            "grid.alpha": 0.55,
            "font.family": "DejaVu Sans",
            "font.size": 11,
            "axes.titleweight": "bold",
            "savefig.facecolor": BG,
            "savefig.edgecolor": BG,
        }
    )


def finish_axis(ax):
    ax.grid(True, linewidth=0.8)
    ax.spines["top"].set_visible(False)
    ax.spines["right"].set_visible(False)
    ax.spines["left"].set_color(GRID)
    ax.spines["bottom"].set_color(GRID)
    ax.yaxis.set_major_locator(MaxNLocator(integer=True))
    ax.margins(x=0.02)


def save(fig, output_dir, filename, tight=True):
    fig.savefig(output_dir / filename, dpi=180, bbox_inches="tight")
    plt.close(fig)


def plot_population(rows, output_dir):
    t = col(rows, "time")
    prey = col(rows, "preyCount")
    predator = col(rows, "predatorCount")

    fig, ax = plt.subplots(figsize=(13.5, 7.2))
    ax.plot(t, prey, color=PREY, linewidth=3.0, label="Prey")
    ax.plot(t, predator, color=PREDATOR, linewidth=3.0, label="Predators")
    ax.fill_between(t, prey, color=PREY, alpha=0.16)
    ax.fill_between(t, predator, color=PREDATOR, alpha=0.14)
    ax.set_title("Predator vs Prey Population", loc="left", pad=16, fontsize=20)
    ax.set_xlabel("Simulation time [s]")
    ax.set_ylabel("Agents")
    ax.legend(facecolor=PANEL, edgecolor=GRID, labelcolor=TEXT, loc="upper right")
    finish_axis(ax)
    save(fig, output_dir, "01_population_predator_vs_prey.png")


def plot_life_stages(rows, output_dir):
    t = col(rows, "time")
    prey_j = col(rows, "preyJuvenileCount")
    prey_a = col(rows, "preyAdultCount")
    pred_j = col(rows, "predatorJuvenileCount")
    pred_a = col(rows, "predatorAdultCount")

    fig = plt.figure(figsize=(13.5, 8.0))
    gs = GridSpec(2, 1, figure=fig, hspace=0.35)
    axes = [fig.add_subplot(gs[0]), fig.add_subplot(gs[1])]

    axes[0].stackplot(t, prey_j, prey_a, colors=[JUVENILE, PREY], alpha=0.82, labels=["Juvenile prey", "Adult prey"])
    axes[0].set_title("Prey Life Stages", loc="left", pad=12, fontsize=17)
    axes[0].legend(facecolor=PANEL, edgecolor=GRID, labelcolor=TEXT, loc="upper right")
    axes[0].set_ylabel("Prey count")
    finish_axis(axes[0])

    axes[1].stackplot(t, pred_j, pred_a, colors=[JUVENILE, PREDATOR], alpha=0.82, labels=["Juvenile predators", "Adult predators"])
    axes[1].set_title("Predator Life Stages", loc="left", pad=12, fontsize=17)
    axes[1].legend(facecolor=PANEL, edgecolor=GRID, labelcolor=TEXT, loc="upper right")
    axes[1].set_xlabel("Simulation time [s]")
    axes[1].set_ylabel("Predator count")
    finish_axis(axes[1])

    save(fig, output_dir, "02_life_stages.png")


def plot_energy(rows, output_dir):
    t = col(rows, "time")
    prey_avg = col(rows, "avgPreyEnergy")
    pred_avg = col(rows, "avgPredatorEnergy")
    prey_min = col(rows, "minPreyEnergy")
    prey_max = col(rows, "maxPreyEnergy")
    pred_min = col(rows, "minPredatorEnergy")
    pred_max = col(rows, "maxPredatorEnergy")

    fig, ax = plt.subplots(figsize=(13.5, 7.2))
    ax.fill_between(t, prey_min, prey_max, color=ENERGY_PREY, alpha=0.10, label="Prey energy range")
    ax.fill_between(t, pred_min, pred_max, color=ENERGY_PREDATOR, alpha=0.10, label="Predator energy range")
    ax.plot(t, prey_avg, color=ENERGY_PREY, linewidth=3.0, label="Avg prey energy")
    ax.plot(t, pred_avg, color=ENERGY_PREDATOR, linewidth=3.0, label="Avg predator energy")
    ax.set_title("Average Energy and Energy Spread", loc="left", pad=16, fontsize=20)
    ax.set_xlabel("Simulation time [s]")
    ax.set_ylabel("Energy")
    ax.legend(facecolor=PANEL, edgecolor=GRID, labelcolor=TEXT, loc="upper right")
    finish_axis(ax)
    save(fig, output_dir, "03_energy.png")


def plot_events(rows, output_dir):
    t = col(rows, "time")
    births = col(rows, "totalBirths")
    deaths = col(rows, "totalDeaths")

    fig, ax = plt.subplots(figsize=(13.5, 7.2))
    ax.plot(t, births, color=BIRTH, linewidth=3, label="Births")
    ax.plot(t, deaths, color=DEATH, linewidth=3, label="Deaths")
    ax.fill_between(t, births, color=BIRTH, alpha=0.12)
    ax.fill_between(t, deaths, color=DEATH, alpha=0.12)
    ax.set_title("Births and Deaths", loc="left", pad=16, fontsize=20)
    ax.set_xlabel("Simulation time [s]")
    ax.set_ylabel("Events")
    ax.legend(facecolor=PANEL, edgecolor=GRID, labelcolor=TEXT, loc="upper left")
    finish_axis(ax)
    save(fig, output_dir, "04_births_deaths.png")


def plot_resources(rows, output_dir):
    t = col(rows, "time")
    grass = col(rows, "totalGrassFood")
    max_grass = col(rows, "maxGrassFood")
    occupancy = col(rows, "shelterOccupancy")
    capacity = col(rows, "shelterCapacity")

    grass_ratio = [(g / m * 100.0) if m > 0 else 0.0 for g, m in zip(grass, max_grass)]
    shelter_ratio = [(o / c * 100.0) if c > 0 else 0.0 for o, c in zip(occupancy, capacity)]

    fig, ax = plt.subplots(figsize=(13.5, 7.2))
    ax.plot(t, grass_ratio, color=GRASS, linewidth=3, label="Grass resource fill")
    ax.plot(t, shelter_ratio, color=SHELTER, linewidth=3, label="Shelter occupancy")
    ax.fill_between(t, grass_ratio, color=GRASS, alpha=0.13)
    ax.fill_between(t, shelter_ratio, color=SHELTER, alpha=0.11)
    ax.set_ylim(0, 105)
    ax.set_title("Environment Resources", loc="left", pad=16, fontsize=20)
    ax.set_xlabel("Simulation time [s]")
    ax.set_ylabel("Percent")
    ax.legend(facecolor=PANEL, edgecolor=GRID, labelcolor=TEXT, loc="upper right")
    finish_axis(ax)
    save(fig, output_dir, "05_environment_resources.png")


def plot_summary(rows, metadata, output_dir):
    first = rows[0]
    last = rows[-1]
    duration = metadata.get("durationSeconds", last.get("time", 0))
    survival_prey = last.get("preyCount", 0) - first.get("preyCount", 0)
    survival_pred = last.get("predatorCount", 0) - first.get("predatorCount", 0)
    labels = ["Prey delta", "Predator delta", "Births", "Deaths"]
    values = [
        survival_prey,
        survival_pred,
        last.get("totalBirths", 0),
        last.get("totalDeaths", 0),
    ]
    colors = [PREY, PREDATOR, BIRTH, DEATH]

    fig, ax = plt.subplots(figsize=(11.5, 7.0))
    bars = ax.bar(labels, values, color=colors, alpha=0.86)
    ax.axhline(0, color=GRID, linewidth=1.5)
    ax.set_title("Simulation Outcome Summary", loc="left", pad=16, fontsize=20)
    ax.set_ylabel("Count")
    for bar, value in zip(bars, values):
        y = bar.get_height()
        offset = 0.6 if y >= 0 else -1.6
        ax.text(
            bar.get_x() + bar.get_width() / 2,
            y + offset,
            f"{int(value)}",
            ha="center",
            va="bottom" if y >= 0 else "top",
            color=TEXT,
            fontweight="bold",
        )
    ax.text(
        0.99,
        0.96,
        f"Duration: {duration:.1f}s\nSamples: {len(rows)}",
        transform=ax.transAxes,
        ha="right",
        va="top",
        color=MUTED,
        bbox={"boxstyle": "round,pad=0.55", "facecolor": BG, "edgecolor": GRID, "alpha": 0.9},
    )
    finish_axis(ax)
    save(fig, output_dir, "00_simulation_summary.png")


def main():
    if len(sys.argv) != 4:
        print("Usage: generate_simulation_charts.py <stats.csv> <metadata.json> <output_dir>", file=sys.stderr)
        return 2

    csv_path = Path(sys.argv[1])
    metadata_path = Path(sys.argv[2])
    output_dir = Path(sys.argv[3])
    output_dir.mkdir(parents=True, exist_ok=True)

    rows = load_rows(csv_path)
    if not rows:
        print("No simulation samples found.", file=sys.stderr)
        return 1

    metadata = {}
    if metadata_path.exists():
        with open(metadata_path, encoding="utf-8-sig") as handle:
            metadata = json.load(handle)

    style()
    plot_summary(rows, metadata, output_dir)
    plot_population(rows, output_dir)
    plot_life_stages(rows, output_dir)
    plot_energy(rows, output_dir)
    plot_events(rows, output_dir)
    plot_resources(rows, output_dir)

    print(f"Generated {len(list(output_dir.glob('*.png')))} simulation charts in {output_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
