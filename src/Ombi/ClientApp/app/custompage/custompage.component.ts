﻿import { Component, OnInit } from "@angular/core";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { AuthService } from "../auth/auth.service";
import { NotificationService, SettingsService } from "../services";

@Component({
    templateUrl: "./custompage.component.html",
    styleUrls: ["./custompage.component.scss"],
})
export class CustomPageComponent implements OnInit {

    public form: FormGroup;
    public isEditing: boolean;
    public isAdmin: boolean;

    constructor(private auth: AuthService, private settings: SettingsService, private fb: FormBuilder,
                private notificationService: NotificationService) {
    }

    public ngOnInit() {
        this.settings.getCustomPage().subscribe(x => {

            this.form = this.fb.group({
                enabled: [x.enabled],
                title: [x.title, [Validators.required]],
                html: [x.html, [Validators.required]],
            });
        });
        this.isAdmin = this.auth.hasRole("admin") || this.auth.hasRole("poweruser");
    }

    public onSubmit() {
        if (this.form.invalid) {
            this.notificationService.error("Please check your entered values");
            return;
        }
        this.settings.saveCustomPage(this.form.value).subscribe(x => {
            if (x) {
                this.notificationService.success("Successfully saved Custom Page settings");
            } else {
                this.notificationService.success("There was an error when saving the Custom Page settings");
            }
        });
    }
}
