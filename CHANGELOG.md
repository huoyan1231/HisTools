# 0.3.0 (Current)
<details>
  <summary>📜 View </summary>

  ## Bug Fixes:
  - **BuffsDisplay**: Fixed countdown timer freezing when multiple timed items are active
    - `SummaryBuffSecondsLeft` now returns the shortest (soonest expiring) timer among all active containers, instead of only reflecting the last container
  - **BuffsDisplay**: Updated timer calculation to use `BuffContainer.buffTime` (the engine-side normalized [0,1] drain timer) for accurate remaining time display after game update
  - **ShowItemInfo**: Fixed compile error — `LookAtPlayer` was renamed to `UT_LookatPlayer` in the game update; the `player` field was also removed as the component now auto-detects the player
</details>

# 0.2.0
<details>
  <summary>📜 View </summary>



  ## New Feature:
  - Added `BuffsDisplay`, a feature that displays all current buff effects (Stacks/Icon/Duration)
  
  <img src="https://github.com/cyfral123/HisTools/blob/master/BuffsDisplay_preview.png?raw=true" alt="BuffsDisplay_preview"/>
</details>  

# 0.1.1
<details>
  <summary>📜 View </summary>
  
  ## RoutePlayer:
  - A silly bug that prevented builtin routes from being loaded from the zip file has been fixed
  
  ## SpeedrunStats:
  - Improved view
  - Added Option `ShowOnlyInPause`
  - Added Option `PredictElapsedTime

  - The `elapsedTime` for a level can now be predicted in real time, and with fairly high accuracy

</details>  

# 0.1.0
<details>
  <summary>📜 View </summary>

  - Mod uploaded to thunderstore

</details>  
