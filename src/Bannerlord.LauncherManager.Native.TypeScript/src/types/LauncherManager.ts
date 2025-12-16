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

// Mod Category Types
export type ModuleCategoryType = 'Uncategorized' | 'Gameplay' | 'Combat' | 'Graphics' | 'UI' | 'Audio' | 'QualityOfLife' | 'Overhaul' | 'Troops' | 'Items' | 'Maps' | 'Quests' | 'Economy' | 'Diplomacy' | 'Cheats' | 'Utility' | 'Framework' | 'Compatibility';

export interface ModuleCategory {
  id: string;
  name: string;
  description?: string;
  color?: string;
  icon?: string;
  isPredefined: boolean;
  sortOrder: number;
}

export interface ModuleTag {
  name: string;
  color?: string;
  createdAt: string;
}

export interface ModuleCategoryAssignment {
  moduleId: string;
  categoryId: string;
  tags: string[];
  notes?: string;
  rating?: number;
  isFavorite: boolean;
}

export interface CategorySummary {
  categoryId: string;
  categoryName: string;
  moduleCount: number;
  enabledCount: number;
}

export interface ModuleFilterOptions {
  categoryId?: string;
  tags?: string[];
  favoritesOnly?: boolean;
  minRating?: number;
  enabledOnly?: boolean;
  searchText?: string;
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

  // Mod Category methods
  getCategoriesAsync(): Promise<ModuleCategory[]>;
  createCategoryAsync(name: string, description?: string, color?: string): Promise<ModuleCategory>;
  deleteCategoryAsync(categoryId: string): Promise<boolean>;
  setModuleCategoryAsync(moduleId: string, categoryId: string): Promise<void>;
  getModuleCategoryAsync(moduleId: string): Promise<ModuleCategory | null>;
  getModulesByCategoryAsync(categoryId: string): Promise<string[]>;
  getTagsAsync(): Promise<ModuleTag[]>;
  createTagAsync(name: string, color?: string): Promise<ModuleTag>;
  deleteTagAsync(tagName: string): Promise<boolean>;
  addModuleTagAsync(moduleId: string, tagName: string): Promise<void>;
  removeModuleTagAsync(moduleId: string, tagName: string): Promise<void>;
  getModuleTagsAsync(moduleId: string): Promise<string[]>;
  getModulesByTagAsync(tagName: string): Promise<string[]>;
  setModuleFavoriteAsync(moduleId: string, isFavorite: boolean): Promise<void>;
  getFavoriteModulesAsync(): Promise<string[]>;
  setModuleRatingAsync(moduleId: string, rating: number): Promise<void>;
  setModuleNotesAsync(moduleId: string, notes?: string): Promise<void>;
  getModuleAssignmentAsync(moduleId: string): Promise<ModuleCategoryAssignment | null>;
  getCategorySummaryAsync(): Promise<CategorySummary[]>;
  filterModulesAsync(options: ModuleFilterOptions): Promise<string[]>;
}
