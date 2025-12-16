import { ModuleInfoExtended, ModuleInfoExtendedWithMetadata } from "./BannerlordModuleManager";

export interface INativeExtension {
  LauncherManager: new (
    setGameParametersAsync: (executable: string, gameParameters: string[]) => Promise<void>,
    sendNotificationAsync: (id: string, type: NotificationType, message: string, delayMS: number) => Promise<void>,
    sendDialogAsync: (type: DialogType, title: string, message: string, filters: FileFilter[]) => Promise<string>,
    getInstallPathAsync: () => Promise<string>,
    readFileContentAsync: (filePath: string, offset: number, length: number) => Promise<Uint8Array | null>,
    writeFileContentAsync: (filePath: string, data: Uint8Array) => Promise<void>,
    readDirectoryFileListAsync: (directoryPath: string) => Promise<string[] | null>,
    readDirectoryListAsync: (directoryPath: string) => Promise<string[] | null>,
    getAllModuleViewModelsAsync: () => Promise<ModuleViewModel[] | null>,
    getModuleViewModelsAsync: () => Promise<ModuleViewModel[] | null>,
    setModuleViewModelsAsync: (moduleViewModels: ModuleViewModel[]) => Promise<void>,
    getOptionsAsync: () => Promise<LauncherOptions>,
    getStateAsync: () => Promise<LauncherState>,
  ) => LauncherManager
}

export interface LoadOrderEntry {
  id: string;
  name: string;
  isSelected: boolean;
  isDisabled: boolean;
  index: number;
}

export interface LoadOrder {
  [id: string]: LoadOrderEntry;
}

export interface ModuleViewModel {
  moduleInfoExtended: ModuleInfoExtendedWithMetadata;
  isValid: boolean;
  isSelected: boolean;
  isDisabled: boolean;
  index: number;
}

export interface LauncherOptions {
  betaSorting: boolean;
}

export interface LauncherState {
  isSingleplayer: boolean;
}

export interface SaveMetadata {
  [key: string]: string;
  Name: string;
}

export type GameStore = 'Steam' | 'GOG' | 'Epic' | 'Xbox' | 'Unknown';
export type GamePlatform = 'Win64' | 'Xbox' | 'Unknown';

export type NotificationType = 'hint' | 'info' | 'warning' | 'error';
export type DialogType = 'warning' | 'fileOpen' | 'fileSave';

export type InstructionType = 'Copy' | 'ModuleInfo' | 'CopyStore';
export interface InstallInstruction {
  type: InstructionType;
  store?: GameStore;
  moduleInfo?: ModuleInfoExtended;
  source?: string;
  destination?: string;
}
export interface InstallResult {
  instructions: InstallInstruction[];
}

export interface SupportedResult {
  supported: boolean;
  requiredFiles: string[];
}

export interface OrderByLoadOrderResult {
  result: boolean;
  issues?: string[];
  orderedModuleViewModels?: ModuleViewModel[]
}

export interface FileFilter {
  name: string;
  extensions: string[];
}

// Preset Sharing Types
export type PresetVisibility = 'Private' | 'Public' | 'Unlisted';
export type PresetApplyStatus = 'Success' | 'PartialSuccess' | 'MissingMods' | 'Failed';

export interface PresetModuleEntry {
  id: string;
  name: string;
  version: string;
  isEnabled: boolean;
  loadOrder: number;
  isRequired: boolean;
  downloadUrl?: string;
  nexusModsId?: string;
  steamWorkshopId?: string;
}

export interface ModPreset {
  id: string;
  name: string;
  description?: string;
  author?: string;
  createdAt: string;
  updatedAt: string;
  gameVersion?: string;
  visibility: PresetVisibility;
  tags: string[];
  modules: PresetModuleEntry[];
  shareCode: string;
  moduleCount: number;
  enabledCount: number;
  formatVersion: number;
}

export interface PresetApplyResult {
  status: PresetApplyStatus;
  applied: string[];
  missing: PresetModuleEntry[];
  versionMismatches: PresetModuleEntry[];
  errorMessage?: string;
  allRequiredFound: boolean;
}

export interface PresetCreateOptions {
  enabledOnly?: boolean;
  includeNative?: boolean;
  includeDownloadUrls?: boolean;
  includeVersions?: boolean;
}

export interface PresetApplyOptions {
  skipMissing?: boolean;
  ignoreVersions?: boolean;
  disableOthers?: boolean;
  createBackup?: boolean;
}

export interface PresetImportResult {
  success: boolean;
  preset?: ModPreset;
  errorMessage?: string;
  wasDuplicate: boolean;
}

export interface PresetComparisonResult {
  onlyInFirst: string[];
  onlyInSecond: string[];
  inBoth: string[];
  differentOrder: string[];
  similarityPercent: number;
}

// Launch Statistics Types
export type SessionOutcome = 'Unknown' | 'Normal' | 'Crash' | 'ForceQuit' | 'Error';

export interface LaunchRecord {
  id: string;
  launchedAt: string;
  endedAt?: string;
  duration: number;
  gameVersion?: string;
  profileId?: string;
  profileName?: string;
  gameMode?: string;
  enabledModules: string[];
  moduleCount: number;
  outcome: SessionOutcome;
  exitCode?: number;
  errorMessage?: string;
}

export interface LaunchStatsSummary {
  totalLaunches: number;
  totalPlayTime: number;
  averageSessionDuration: number;
  longestSession: number;
  totalCrashes: number;
  crashRate: number;
  firstLaunch?: string;
  lastLaunch?: string;
  mostUsedProfile?: string;
  launchesPerDay: number;
}

export interface ModuleUsageStats {
  moduleId: string;
  moduleName: string;
  usageCount: number;
  totalPlayTime: number;
  crashCount: number;
  crashRate: number;
  lastUsed?: string;
}

export interface CrashCorrelation {
  moduleId: string;
  moduleName: string;
  crashCount: number;
  totalLaunches: number;
  crashRate: number;
  relativeCrashRate: number;
  confidence: string;
}

export interface StatisticsTimeRange {
  startDate?: string;
  endDate?: string;
  lastDays?: number;
}

export type LauncherManager = {
  constructor(): LauncherManager;

  checkForRootHarmonyAsync(): Promise<void>;
  getGamePlatformAsync(): Promise<GamePlatform>;
  getGameVersionAsync(): Promise<string>;
  getModulesAsync(): Promise<ModuleInfoExtendedWithMetadata[]>;
  getAllModulesAsync(): Promise<ModuleInfoExtendedWithMetadata[]>;
  getSaveFilePathAsync(saveFile: string): Promise<string>;
  getSaveFilesAsync(): Promise<SaveMetadata[]>;
  getSaveMetadataAsync(saveFile: string, data: Uint8Array): Promise<SaveMetadata>;
  installModule(files: string[], moduleInfos: ModuleInfoExtendedWithMetadata[]): InstallResult;
  isObfuscatedAsync(module: ModuleInfoExtendedWithMetadata): Promise<boolean>;
  isSorting(): boolean;
  moduleListHandlerExportAsync(): Promise<void>;
  moduleListHandlerExportSaveFileAsync(saveFile: string): Promise<void>;
  moduleListHandlerImportAsync(): Promise<boolean>;
  moduleListHandlerImportSaveFileAsync(saveFile: string): Promise<boolean>;
  orderByLoadOrderAsync(loadOrder: LoadOrder): Promise<OrderByLoadOrderResult>;
  refreshModulesAsync(): Promise<void>;
  refreshGameParametersAsync(): Promise<void>;
  setGameParameterExecutableAsync(executable: string): Promise<void>;
  setGameParameterSaveFileAsync(saveName: string): Promise<void>;
  setGameParameterContinueLastSaveFileAsync(value: boolean): Promise<void>;
  setGameStore(gameStore: GameStore): void;
  sortAsync(): Promise<void>;
  sortHelperChangeModulePositionAsync(moduleViewModel: ModuleViewModel, insertIndex: number): Promise<boolean>;
  sortHelperToggleModuleSelectionAsync(moduleViewModel: ModuleViewModel): Promise<ModuleViewModel>;
  sortHelperValidateModuleAsync(moduleViewModel: ModuleViewModel): Promise<string[]>;
  testModule(files: string[]): SupportedResult;

  dialogTestWarningAsync(): Promise<string>;
  dialogTestFileOpenAsync(): Promise<string>;

  setGameParameterLoadOrderAsync(loadOrder: LoadOrder): Promise<void>;

  // Launch Statistics methods
  recordLaunchAsync(): Promise<LaunchRecord>;
  recordSessionEndAsync(outcome: SessionOutcome, exitCode?: number, errorMessage?: string): Promise<void>;
  getLaunchHistoryAsync(range?: StatisticsTimeRange): Promise<LaunchRecord[]>;
  getStatsSummaryAsync(range?: StatisticsTimeRange): Promise<LaunchStatsSummary>;
  getTotalPlayTimeAsync(range?: StatisticsTimeRange): Promise<number>;
  getModuleUsageStatsAsync(range?: StatisticsTimeRange): Promise<ModuleUsageStats[]>;
  getCrashCorrelationsAsync(): Promise<CrashCorrelation[]>;
  getRecentLaunchesAsync(count?: number): Promise<LaunchRecord[]>;
  getLaunchesForProfileAsync(profileId: string): Promise<LaunchRecord[]>;
  getPlayTimeForProfileAsync(profileId: string): Promise<number>;
  clearStatisticsAsync(): Promise<void>;
  exportStatisticsAsync(): Promise<string>;
  getDailyLaunchCountsAsync(days?: number): Promise<Record<string, number>>;

  // Preset Sharing methods
  createPresetAsync(name: string, description?: string, options?: PresetCreateOptions): Promise<ModPreset>;
  getPresetsAsync(): Promise<ModPreset[]>;
  getPresetByIdAsync(presetId: string): Promise<ModPreset | null>;
  getPresetByShareCodeAsync(shareCode: string): Promise<ModPreset | null>;
  updatePresetAsync(presetId: string, name?: string, description?: string, tags?: string[]): Promise<boolean>;
  refreshPresetAsync(presetId: string, options?: PresetCreateOptions): Promise<ModPreset | null>;
  deletePresetAsync(presetId: string): Promise<boolean>;
  applyPresetAsync(presetId: string, options?: PresetApplyOptions): Promise<PresetApplyResult>;
  exportPresetAsync(presetId: string): Promise<string>;
  exportPresetToFileAsync(presetId: string, filePath: string): Promise<boolean>;
  importPresetAsync(json: string): Promise<PresetImportResult>;
  importPresetFromFileAsync(filePath: string): Promise<PresetImportResult>;
  getImportedPresetsAsync(): Promise<ModPreset[]>;
  toggleFavoriteAsync(presetId: string): Promise<boolean>;
  getFavoritePresetsAsync(): Promise<ModPreset[]>;
  comparePresetsAsync(presetId1: string, presetId2: string): Promise<PresetComparisonResult>;
  getLastAppliedPresetAsync(): Promise<ModPreset | null>;
  getShareCodeAsync(presetId: string): Promise<string>;
}
