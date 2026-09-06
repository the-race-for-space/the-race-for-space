# Development To-Do

This file tracks current development ideas and polish work only. Completed items are removed once finished; implementation history remains available in Git and the current project documentation. Items are not listed in priority order unless stated otherwise.

## Overview

- [ ] **Hide achieved objectives from Your Objectives.** The `Your Objectives` list should show only objectives the player has not yet achieved; completed objectives should no longer remain in that list.
- [ ] **Show `None` when there are no current objectives.** If filtering completed or unavailable objectives leaves `Your Objectives` empty, show an explicit `None` state instead of leaving the section blank.
- [ ] **Show remaining one-off funding payments.** For each completed Objective Funding Contract shown in Overview funding information, display how many of its ten scheduled payments remain, updating after each funding payout.
- [ ] **Show `None` when there are no Satellite Contracts.** When the Satellite Contracts or satellite-network summary has no contracts to display, show an explicit `None` state instead of leaving the section empty or ambiguous.

## Funding Targets

- [ ] **Order Funding Targets by player completion state.** Show objectives the player has not completed at the top of the list, with player-completed objectives moved to the bottom so current work is easier to find.

## Contract Catalogue

- [ ] **Display counts in catalogue section headings.** Show the number of contracts in each section heading, for example `Offered (4)`, `Unlocked (2)`, `Locked (20)`, and `Expired (1)`.

## Flight UI

- [ ] **Display funding reward beside each contract.** Show the contract's funding reward in the compact Offered Contracts window so the player can see the value of the objective without opening the full Command Center.

## Notifications

- [ ] **Announce completed sponsor reviews.** When a sponsor review makes new funding targets `Offered`, notify the player and identify the newly offered targets.
- [ ] **Announce rival objective completions.** Notify the player when a rival agency completes an objective so important competitive progress is visible without continuously checking the Rival Agencies view.
- [ ] **Announce funding payouts received.** When a funding boundary pays the player's agency, notify the player of the funds received from the campaign funding system.

## Funding information polish

- [ ] **Show projected player share percentage.** Where one-off contract payout information is displayed, show the player's projected percentage share of the next payout as well as the funds amount.
- [ ] **Show the current number of eligible agencies.** Show how many agencies currently qualify to share the next one-off objective funding payout so the displayed player share is easier to understand.

## Help / tutorial

- [ ] **Add a Pre-Orbit line quick guide.** Add a short player-facing explanation of Directed Power, Mass, Control, and Biome, including the basic purpose of each progression line.
- [ ] **Explain funding sharing with one worked example.** Add one simple example showing how a one-off objective payout is shared when multiple agencies are eligible, so the declining funding and competition rules are easier to understand.

## Config / balance

- [ ] **Make Pre-Orbit rewards configurable.** Move the Level I-V Pre-Orbit rewards from code-owned values into `CampaignSettings.cfg`, preserving the current 10,000 / 20,000 / 30,000 / 40,000 / 50,000 funds progression as the default.
- [ ] **Make Pre-Orbit rival progress costs configurable.** Move the Level I-V Pre-Orbit rival progress costs into `CampaignSettings.cfg`, preserving the current 4,000 / 6,000 / 8,000 / 10,000 / 12,000 funds progression as the default.
