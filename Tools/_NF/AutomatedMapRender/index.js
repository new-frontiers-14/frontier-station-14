const path = require("path");

// Script Made by Myzumi
// Edit Variables here
const ShowContainerLogs = true; // Set to false to hide container logs
const ShipyardPath = path.join(__dirname, "..", "..", "..", "Resources", "Prototypes", "_NF", "Shipyard"); // Path to the shuttle files
const ShipRootPath = path.join(__dirname, "..", "..", "..", "Resources", "Maps", "_NF", "Shuttles"); // Path to the shuttle files
let MaxInstances = 1; // Maximum number of instances to run in parallel

// !! Do not edit below this line if you don't know what you're doing !!
// Developer Settings;
let Debug = true; // Set to true to enable debug mode, which will skip the 10 second wait and show more logs
const SkipBuild = false; // Set to true to skip the build process of the MapRenderer, Not Recommended due to the required Toolbox Fixes
//
const { exec } = require("child_process");
const { processRobustFiles } = require("./RobustToolboxFixes.js");
const fs = require("fs");
const YAML = require("yaml");
const chalk = require("chalk");
const Root = path.join(__dirname, "..", "..", "..");
const Logs = {};
let LockQueueClear = false;
const ShuttlePaths = {};

// Пути для сохранения данных
const WEB_SITE_ROOT = "/var/www/shipyard_web_usr/data/www/shipyard.frontierstation14.com";
const WEB_SHUTTLES_JSON = path.join(WEB_SITE_ROOT, "storage", "app", "data", "pages", "shuttles.json");
const WEB_RENDERS_DIR = path.join(WEB_SITE_ROOT, "storage", "app", "public", "renders", "shuttles");

let DevFilter = []; // This should not be used. Only for testing purposes.

let SucceedShuttles = [];
let EditedShuttles = [];
let FailedShuttles = [];

const Tags = {
    info: chalk.bgWhite("[INFO]") + " ",
    error: chalk.bgRed("[ERROR]") + " ",
    warning: chalk.bgYellow("[WARNING]") + " ",
    debug: chalk.bgRed("[DEBUG]") + " ",
};
console.log(chalk.bgRed(chalk.yellow(`${chalk.bold("WARNING:")} This script will modify the RobustToolbox files!`)));
console.log(
    chalk.bgRed(chalk.yellow(`The Script will modify the EntityDeserializer.cs and MapLoaderSystem.Load.cs files!`))
);
console.log(chalk.bgRed(chalk.yellow(`${chalk.bold("WARNING:")} This script will modify Ship files!`)));
console.log(
    chalk.bgRed(chalk.yellow(`The Script will modify Ship Mapping files and ${chalk.bold("WILL")} screw them up!`))
);
console.log(chalk.bgRed(chalk.yellow(`The Script will continue in 15 seconds. Press Ctrl+C to cancel.`)));
if (process.env.GITHUB_ACTIONS) {
    console.log(
        Tags.info + chalk.yellow(`This script is running in a GitHub Actions environment. Forcing Debug mode.`)
    );
    Debug = true;
    MaxInstances = 1;
}
if (process.env.ENABLE_DEBUG) {
    console.log(Tags.info + chalk.yellow("Debug mode enabled by environment variable."));
    Debug = true;
}
if (process.env.MAX_INSTANCES) {
    console.log(Tags.info + chalk.yellow(`Max instances set to ${process.env.MAX_INSTANCES} by environment variable.`));
    MaxInstances = parseInt(process.env.MAX_INSTANCES);
    if (isNaN(MaxInstances) || MaxInstances <= 0) {
        console.log(Tags.error + chalk.red("Invalid value for MAX_INSTANCES, defaulting to 2."));
        MaxInstances = 2;
    }
}
setTimeout(
    async () => {
        CleanUps();
        await processRobustFiles();
        init();
    },
    Debug ? 0 : 15000
);

function CleanUps() {
    if (fs.existsSync(path.join(__dirname, "ShuttleRenders")))
        fs.rmSync(path.join(__dirname, "ShuttleRenders"), { recursive: true, force: true });

    if (fs.existsSync(path.join(__dirname, "ShipyardData.json")))
        fs.rmSync(path.join(__dirname, "ShipyardData.json"), { recursive: true, force: true });

    if (fs.existsSync(path.join(__dirname, "statistic.json")))
        fs.rmSync(path.join(__dirname, "statistic.json"), { recursive: true, force: true });

    if (fs.existsSync(path.join(__dirname, "ShuttleBackups")))
        fs.rmSync(path.join(__dirname, "ShuttleBackups"), { recursive: true, force: true });

    if (!fs.existsSync(path.join(__dirname, "ShuttleBackups")))
        fs.mkdirSync(path.join(__dirname, "ShuttleBackups"), { recursive: true });

    if (!fs.existsSync(path.join(__dirname, "ShuttleRenders")))
        fs.mkdirSync(path.join(__dirname, "ShuttleRenders"), { recursive: true });

    // Создаем серверную директорию для рендеров
    try {
        fs.mkdirSync(WEB_RENDERS_DIR, { recursive: true });
        console.log(Tags.info + chalk.green(`Server render directory created: ${WEB_RENDERS_DIR}`));
    } catch (error) {
        console.log(Tags.warning + chalk.yellow(`Could not create server render directory: ${error.message}`));
    }
}
async function init() {
    let ShipyardTypes = await FindShuttleFiles(ShipyardPath);
    const AllShuttleToRender = [];
    const ShuttlesData = {
        renders_path: "renders/shuttles/",
        categories: {},
    };

    ShipyardTypes.forEach((file) => {
        if (String(file).toLowerCase().includes("base")) return;
        let fileName = String(file).split("/").pop().toLowerCase();
        if (DevFilter.length !== 0 && !DevFilter.includes(String(fileName.replace(".yml", "")))) {
            if (Debug) console.log(Tags.debug + chalk.cyan(`Ignoring Shipyard File: ${file}`));
            return;
        }
        if (Debug) console.log(Tags.debug + chalk.cyan(`Found Shipyard File: ${file}`));
        const filePath = path.join(ShipyardPath, file);
        const fileContent = fs.readFileSync(filePath, "utf8");
        const yamlData = YAML.parse(fileContent, { logLevel: "error" });
        if (!yamlData[0] || yamlData[0].type !== "vessel") return;

        const vesselData = yamlData[0];
        const shuttleId = vesselData.id;
        const shuttleIdLower = shuttleId.toLowerCase();
        const posixDir = path.posix.dirname(String(file).replace(/\\/g, "/"));
        const folderGroup = posixDir !== "." ? posixDir.split("/").pop() : null;
        const group = vesselData.group || folderGroup || "Unknown";

        const shuttleItem = {
            id: shuttleId,
            name: vesselData.name || shuttleId,
            image: `${shuttleIdLower}.png`,
            price: vesselData.price || 0,
            description: vesselData.description || "",
            category: vesselData.category || "Small",
            class: vesselData.class || [],
            engines: vesselData.engine || [],
        };

        if (!ShuttlesData.categories[group]) {
            ShuttlesData.categories[group] = [];
        }
        ShuttlesData.categories[group].push(shuttleItem);
        AllShuttleToRender.push(file);
        const relativePath = path.relative(__dirname, path.join(ShipRootPath, file)).replace(/\\/g, "/");
        ShuttlePaths[file.toLowerCase()] = relativePath;
        ShuttlePaths[fileName] = relativePath;
    });

    if (AllShuttleToRender.length === 0) {
        console.log(Tags.error + chalk.red(`No Shuttles were found inside ${ShipyardPath}, exiting...`));
        return process.exit(1);
    }

    // Сохраняем ShipyardData.json локально для отладки
    fs.writeFileSync(path.join(__dirname, "ShipyardData.json"), JSON.stringify(ShuttlesData, null, 2), "utf8");

    // Сохраняем на сервер
    try {
        const serverDir = path.dirname(WEB_SHUTTLES_JSON);
        fs.mkdirSync(serverDir, { recursive: true });
        fs.writeFileSync(WEB_SHUTTLES_JSON, JSON.stringify(ShuttlesData, null, 4), "utf8");
        console.log(Tags.info + chalk.green(`shuttles.json saved: ${WEB_SHUTTLES_JSON}`));
    } catch (error) {
        console.log(Tags.error + chalk.red(`Failed to save shuttles.json: ${error.message}`));
    }

    let IsMapRendererBuilt = false;

    if (!SkipBuild) {
        console.log(chalk.yellow("Building MapRenderer..."));
        const BuildMapRenderer = exec(`cd ${Root} && dotnet build Content.MapRenderer/Content.MapRenderer.csproj`, { maxBuffer: 1024 * 1024 * 1024 });
        AddExecLogs(BuildMapRenderer, "[MapRenderer]");
        BuildMapRenderer.on("close", () => {
            IsMapRendererBuilt = true;
            console.log(chalk.green("MapRenderer built successfully!"));
            console.log(chalk.yellow("Starting MapRenderer..."));
        });
    } else IsMapRendererBuilt = true;

    let CurrentInstances = 0;

    const Queue = setInterval(() => {
        if (!IsMapRendererBuilt) {
            if (Debug) console.log(Tags.debug + chalk.cyan("MapRenderer is not built yet, waiting..."));
            return;
        }
        if (AllShuttleToRender.length === 0 && CurrentInstances === 0) {
            if (LockQueueClear)
                return console.log(
                    Tags.warning + chalk.yellow("Another Action is requiring a QueueLock, waiting for Action to end.")
                );
            clearInterval(Queue);
            if (EditedShuttles.length !== 0) {
                EditedShuttles.forEach((shuttle) => {
                    const shuttleFile = shuttle + ".yml";
                    // Найдем относительный путь по имени файла или полному пути
                    const RelativePath =
                        ShuttlePaths[shuttleFile.toLowerCase()] || ShuttlePaths[shuttle.toLowerCase() + ".yml"];
                    if (RelativePath) {
                        const BrokenShipPath = path.join(__dirname, RelativePath);
                        fs.rmSync(BrokenShipPath, { recursive: true, force: true });
                        const BackupPath = path.join(__dirname, "ShuttleBackups", shuttleFile);
                        const BackupFile = fs.readFileSync(BackupPath, "utf8");
                        fs.writeFileSync(BrokenShipPath, BackupFile, "utf8");
                        fs.rmSync(BackupPath, { recursive: true, force: true });
                        console.log(Tags.info + chalk.green(`Restored ${shuttleFile} from backup`));
                    } else {
                        console.log(Tags.error + chalk.red(`Cannot find path for shuttle backup: ${shuttleFile}`));
                    }
                });
            }
            console.log(Tags.info + chalk.green("All shuttles have been rendered and it was cleaned up, Exiting..."));
            fs.writeFileSync(
                path.join(__dirname, "statistic.json"),
                JSON.stringify({ succeed: SucceedShuttles, edited: EditedShuttles, failed: FailedShuttles }, null, 2),
                "utf8"
            );
            return;
        }
        if (CurrentInstances < MaxInstances) {
            if (AllShuttleToRender.length === 0) {
                if (Debug)
                    console.log(
                        Tags.debug +
                            chalk.cyan("No Shuttles left to start rendering or last shuttles are still rendering...")
                    );
                return;
            }
            let NextShipyardPath = AllShuttleToRender.shift();
            let ShuttleToRender = NextShipyardPath; // Используем полный путь вместо только имени файла
            let ShuttleName = NextShipyardPath.split("/").pop(); // Имя файла для отображения
            console.log(
                chalk.blue(
                    `Starting MapRenderer for ${ShuttleName.split(".")[0]}, Taking ${PrettyPrintNumber(
                        CurrentInstances + 1
                    )} Slot, now at ${CurrentInstances + 1}/${MaxInstances} Instances, ${
                        AllShuttleToRender.length
                    } left to render`
                )
            );
            const mapPath = `Maps/_NF/Shuttles/${ShuttleToRender}`;
            const outputDir = fs.existsSync(WEB_RENDERS_DIR) ? WEB_RENDERS_DIR : path.join(__dirname, "ShuttleRenders");
            const Command = `cd ${Root} && dotnet run --project Content.MapRenderer --files Resources/${mapPath} --output ${outputDir}`;
            ShuttleToRender = ShuttleName.split(".")[0]; // Оставляем только имя для дальнейшего использования
            if (Debug)
                console.log(
                    Tags.debug + chalk.cyan(`[${CurrentInstances + 1}-Render] Running ChildExec Command: ${Command}`)
                );
            const RenderShuttle = exec(Command, { maxBuffer: 1024 * 1024 * 1024 });
            AddExecLogs(RenderShuttle, `[#${CurrentInstances + 1}-Renderer-${ShuttleToRender}]`, ShuttleToRender);
            CurrentInstances++;
            RenderShuttle.on("close", () => {
                if (Debug)
                    console.log(
                        Tags.debug +
                            chalk.cyan(
                                `Instance ${ShuttleToRender} has finished, deducting one Instance. Now: ${CurrentInstances}/${MaxInstances} Instances`
                            )
                    );
                CurrentInstances--;
                if (!FailedShuttles.includes(ShuttleToRender)) {
                    console.log(Tags.info + chalk.green(`Finished MapRenderer for ${ShuttleToRender}`));
                } else {
                    if (EditedShuttles.includes(ShuttleToRender.split("/").pop())) {
                        console.log(
                            Tags.warning +
                                chalk.yellow(
                                    `MapRenderer for ${ShuttleToRender} failed, but was already edited. Skipping Fixing process`
                                )
                        );
                        return;
                    }
                    console.log(
                        Tags.warning +
                            chalk.yellow(`child process failed, shuttle: ${ShuttleToRender}. Launching Fixing process`)
                    );
                    LockQueueClear = true;
                    let response = FixMappingFile(NextShipyardPath);
                    if (response) {
                        FailedShuttles = FailedShuttles.filter((shuttle) => shuttle !== response);
                        EditedShuttles.push(response);
                        // Восстанавливаем полный путь для повторного рендеринга
                        let fullPath = NextShipyardPath.includes("/") ? NextShipyardPath : response + ".yml";
                        AllShuttleToRender.push(fullPath);
                        LockQueueClear = false;
                    }
                }
            });
        } else {
            if (Debug)
                console.log(
                    Tags.debug +
                        chalk.cyan(
                            `Max instances reached, waiting for next slot. (${CurrentInstances}/${MaxInstances})`
                        )
                );
        }
    }, 5000);
}

async function FindShuttleFiles(folderPath, shuttleFiles = [], rootFolder = folderPath) {
    const contents = fs.readdirSync(folderPath);

    for (const file of contents) {
        const filePath = path.join(folderPath, file);
        if (fs.statSync(filePath).isDirectory()) {
            await FindShuttleFiles(filePath, shuttleFiles, rootFolder);
        } else {
            const relativePath = path.relative(rootFolder, filePath).replace(/\\/g, "/"); // Normalize to forward slashes
            shuttleFiles.push(relativePath);
        }
    }

    return shuttleFiles;
}

let ErrorTiggers = ["System.ArgumentException", "[ERRO]"];

function AddExecLogs(exec, prefix = null, shuttle = null) {
    if (!ShowContainerLogs) return;

    function logData(data) {
        data = data.toString().trim();
        if (!data) return;
        if (!Logs[shuttle]) Logs[shuttle] = [];
        Logs[shuttle].push(data);

        let color = "gray";
        if (ErrorTiggers.some((trigger) => data.includes(trigger))) {
            color = "bgRed";
            prefix = Tags.error + (prefix || "");
            if (shuttle && !FailedShuttles.includes(shuttle)) {
                FailedShuttles.push(shuttle);
            }
        }

        console.log(chalk[color](`${prefix ? `${prefix} ` : ""}${data}`));
    }

    exec.stdout.on("data", logData);
    exec.stderr.on("data", logData);

    exec.on("close", (code) => {
        const color = "gray";
        console.log(chalk[color](`${prefix ? `${prefix} ` : ""}child process exited with code ${code}`));
        if (!FailedShuttles.includes(shuttle) && shuttle) {
            SucceedShuttles.push(shuttle);
            RenameMappedFile(shuttle);
            delete Logs[shuttle];
        }
    });
}

function RenameMappedFile(shuttle) {
    const baseRenderDir = fs.existsSync(WEB_RENDERS_DIR) ? WEB_RENDERS_DIR : path.join(__dirname, "ShuttleRenders");

    const ShuttleName = shuttle.split(".")[0];
    const ShuttleNameLower = ShuttleName.toLowerCase();
    let ShuttleFile = null;
    let ShipyardPath = null;

    if (!fs.existsSync(baseRenderDir)) {
        console.log(Tags.error + chalk.red(`Base render directory not found: ${baseRenderDir}`));
        return;
    }

    const dirs = fs.readdirSync(baseRenderDir, { withFileTypes: true });
    for (const dir of dirs) {
        if (!dir.isDirectory()) continue;

        const dirName = dir.name;
        const dirPath = path.join(baseRenderDir, dirName);

        if (dirName.toLowerCase() !== ShuttleNameLower) continue;

        const files = fs.readdirSync(dirPath);
        const pngFile = files.find((file) => file.endsWith(".png") && file.toLowerCase().includes(ShuttleNameLower));

        if (pngFile) {
            ShipyardPath = dirPath;
            ShuttleFile = path.join(dirPath, pngFile);
            break;
        }
    }

    if (!ShuttleFile || !fs.existsSync(ShuttleFile)) {
        console.log(Tags.error + chalk.red(`Failed to find the rendered file for ${ShuttleName}`));
        return;
    }

    const targetFile = path.join(baseRenderDir, `${ShuttleNameLower}.png`);
    try {
        fs.copyFileSync(ShuttleFile, targetFile);
        fs.rmSync(ShipyardPath, { recursive: true, force: true });
        console.log(Tags.info + chalk.green(`Moved and renamed file: ${targetFile}`));
    } catch (error) {
        console.log(Tags.error + chalk.red(`Failed to move file: ${error.message}`));
    }
}

function FixMappingFile(shuttle) {
    const originalShuttle = shuttle; // Сохраняем оригинальный путь
    const shuttleFileName = shuttle.split("/").pop(); // Получаем только имя файла
    // Попробуем найти путь по полному пути или по имени файла
    let RelativePath = ShuttlePaths[shuttle.toLowerCase()] || ShuttlePaths[shuttleFileName.toLowerCase()];

    if (!RelativePath) {
        console.log(Tags.error + chalk.red(`Cannot find relative path for shuttle: ${shuttle}`));
        return null;
    }

    const BrokenShipyardPath = path.join(__dirname, RelativePath);
    const ShuttleName = shuttleFileName.split(".")[0];
    let ShuttleFile = fs.readFileSync(BrokenShipyardPath, "utf8");
    fs.writeFileSync(path.join(__dirname, "ShuttleBackups", shuttleFileName), ShuttleFile, "utf8");
    let ParsedFile = parseShuttle(ShuttleFile);
    let EditedShuttle = EditShuttle(
        ParsedFile,
        ShuttleName.replace(/_/g, " ").replace(/^./, (str) => str.toUpperCase())
    );
    fs.writeFileSync(BrokenShipyardPath, YAML.stringify(EditedShuttle), "utf8");
    console.log(Tags.info + chalk.cyan(`Edited Mapping File for ${ShuttleName}, trying to re-query MapRenderer`));
    return ShuttleName;
}

function EditShuttle(shuttle, ShuttleID) {
    let BecomesStation = shuttle["entities"][0]["entities"][0]["components"].find(
        (component) => component.type === "BecomesStation"
    );
    if (!BecomesStation) {
        shuttle["entities"][0]["entities"][0]["components"].unshift({ type: "BecomesStation", id: ShuttleID });
    } else {
        if (BecomesStation.id !== ShuttleID) {
            BecomesStation.id = ShuttleID;
        }
    }
    return shuttle;
}

function parseShuttle(data) {
    const doc = YAML.parseDocument(data, {
        strict: false,
        customTags: [
            {
                tag: "!type:SoundPathSpecifier",
                test: /^/,
                resolve(doc, node) {
                    // Always return an empty string if there's no scalar value
                    return node.strValue || "";
                },
            },
        ],
    });

    // Filter out the unresolved-tag warning
    doc.warnings = doc.warnings.filter((w) => w.code !== "TAG_RESOLVE_FAILED");
    return doc.toJSON();
}

function PrettyPrintNumber(number) {
    if (number === 1) return `${number}st`;
    if (number === 2) return `${number}nd`;
    if (number === 3) return `${number}rd`;
    return `${number}th`;
}
