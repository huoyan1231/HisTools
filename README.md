
<img src="https://github.com/cyfral123/HisTools/blob/master/showcase.png?raw=true" alt="Showcase"/>

# How to use
The default key to open the features menu is `Right Shift`. You can change it in the config, look for it in your mod manager. (You can also change the color palette there)

Everything is configured optimally by default, but you can adjust a features settings by clicking the `wrench icon` next to its toggle button.

# Route player

### 🔹 The pathline seems too faint. How can I make it more visible?
You can adjust the transparency and colors in the featuress settings by clicking the `wrench icon`

### 🔹 Why dont my configured colors apply to the route
By default, the `Use route preferred colors` setting is enabled.

If the route author has set preferred colors, they will override your custom colors.

You can either disable this setting or edit the routes colors directly in its json file.

### 🔹 How to Enable/Disable a specific route?
look to the route label and press the `middle mouse button` to toggle the route.

### 🔹 Where are the routes stored, and how can I edit one?

all routes storing in `bepinex/histools/routes` 

Example for Gale mod manager: `C:\Users\your_name\AppData\Roaming\com.kesomannen.gale\white-knuckle\profiles\Default\BepInEx\HisTools\Routes`

You can find route .json file and delete or edit it. 

### 🔹 What are the subfolders inside Routes for?
The folder structure inside `Routes` does not affect how routes work, its purely for convenience.

# Route recorder
### 🔹 What are the "diamonds" that appear while recording?
These are markers for your jumps. Each jump creates a new marker.

### 🔹 How can I stop the recording?
Either disable the feature manually, it will save your recording, or enable `Auto-stop` so that the feature turns off automatically near the level exit (distance is configurable), saving your recording.

### 🔹 Can I add Note (text label) while recording?
You need to hover over the spot where you want to place a note and press the middle mouse button.

After finishing the recording, you can edit the created note in the routes json file

# SpeedrunStats
### 🔹 Why are my average and best times showing as 00:00:00?
You need to play more levels so data can be collected.
You can view the runs history in `BepInEx\HisTools\SpeedrunStats\`.
